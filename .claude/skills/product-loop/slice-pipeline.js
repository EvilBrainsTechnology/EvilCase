export const meta = {
  name: 'slice-pipeline',
  description: 'Run EvilCase slices: architect plans, coder implements, reviewer fixes; a fast slice is the coder alone',
  phases: [
    { title: 'Plan', detail: 'architect plans the slice' },
    { title: 'Implement', detail: 'coder implements through to the pull request' },
    { title: 'Review', detail: 'reviewer reviews and fixes' },
  ],
}

// args may arrive as a JSON string.
const slices = typeof args === 'string' ? JSON.parse(args) : args

const META =
  '\n\nNever edit .claude/** or docs/sdd/** and never create .claude/allow-meta-edits; code ' +
  'that falsifies an instruction or an SDD opens an issue for the owner instead.'

const PR_SCHEMA = {
  type: 'object',
  properties: {
    pr: { type: 'integer', description: 'The opened pull request number' },
    branch: { type: 'string' },
    summary: { type: 'string', description: 'One sentence on what the slice changed' },
  },
  required: ['pr', 'branch', 'summary'],
}

const REVIEW_SCHEMA = {
  type: 'object',
  properties: {
    fixed: { type: 'string', description: 'One sentence on what the review fixed, or "nothing"' },
    uncertainty: { type: 'string', description: 'The one sentence left for the owner, or empty' },
  },
  required: ['fixed'],
}

const results = await pipeline(
  slices,
  async (slice) => {
    // The fast lane skips the plan and raises the coder above its own model and effort.
    if (slice.fast) {
      return agent(
        `Fast lane: you are the only agent on this change — no plan, no review.\n\n` +
          `Implement issue #${slice.issue} "${slice.title}" on branch loop/${slice.issue}-${slice.slug}.\n\n${slice.body}${META}`,
        {
          agentType: 'coder', isolation: 'worktree', phase: 'Implement',
          label: `fast:#${slice.issue}`, schema: PR_SCHEMA, model: 'opus', effort: 'xhigh',
        },
      )
    }
    const plan = await agent(
      `Plan the slice for issue #${slice.issue} "${slice.title}" (branch loop/${slice.issue}-${slice.slug}).\n\n${slice.body}`,
      { agentType: 'architect', phase: 'Plan', label: `plan:#${slice.issue}` },
    )
    if (!plan) throw new Error(`slice #${slice.issue}: no plan`)
    const prompt =
      `Implement issue #${slice.issue} "${slice.title}" on branch loop/${slice.issue}-${slice.slug}.\n\n${slice.body}\n\n` +
      `The architect's plan:\n\n${plan}${META}`
    return agent(prompt, {
      agentType: 'coder', isolation: 'worktree', phase: 'Implement',
      label: `code:#${slice.issue}`, schema: PR_SCHEMA,
    })
  },
  (pr, slice) => {
    if (!pr) throw new Error(`slice #${slice.issue}: no pull request`)
    if (slice.fast) {
      return {
        issue: slice.issue, pr: pr.pr, branch: pr.branch, summary: pr.summary,
        fixed: null, uncertainty: null, status: 'fast',
      }
    }
    return agent(`Review pull request #${pr.pr} and fix what you find.${META}`, {
      agentType: 'reviewer', isolation: 'worktree', phase: 'Review',
      label: `review:#${pr.pr}`, schema: REVIEW_SCHEMA,
    }).then((review) => ({
      issue: slice.issue,
      pr: pr.pr,
      branch: pr.branch,
      summary: pr.summary,
      fixed: review?.fixed ?? null,
      uncertainty: review?.uncertainty || null,
      status: review ? 'reviewed' : 'review-failed',
    }))
  },
)

return slices.map((slice, i) => results[i] ?? { issue: slice.issue, status: 'failed' })
