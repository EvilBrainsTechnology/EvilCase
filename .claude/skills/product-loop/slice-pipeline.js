export const meta = {
  name: 'slice-pipeline',
  description: 'Run EvilCase slices: architect plans, coder implements, reviewer fixes; a fast slice is the coder alone',
  phases: [
    { title: 'Plan', detail: 'architect plans the slice, unless the owner approved a proposal' },
    { title: 'Implement', detail: 'coder implements through to the pull request' },
    { title: 'Review', detail: 'review, security review and screenshots side by side' },
    { title: 'Close', detail: 'review comment and agent-done' },
  ],
}

// args may arrive as a JSON string.
const slices = typeof args === 'string' ? JSON.parse(args) : args

const META =
  '\n\nNever edit .claude/** and never create .claude/allow-meta-edits. One exception: where ' +
  'the plan or issue body carries an applied owner decision changing a governing SDD or the ' +
  'vision, run `touch .claude/allow-meta-edits` in your worktree, make that edit, delete the ' +
  'flag. Any other code that falsifies an instruction or an SDD opens an issue for the owner.'

const PR_SCHEMA = {
  type: 'object',
  properties: {
    pr: { type: 'integer', description: 'The opened pull request number' },
    branch: { type: 'string' },
    summary: { type: 'string', description: 'One sentence on what the slice changed' },
    screens: { type: 'string', description: 'The screenshot targets where a screen changes, or empty' },
    security: { type: 'boolean', description: 'Whether authentication or file storage changed' },
  },
  required: ['pr', 'branch', 'summary', 'screens', 'security'],
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
        `Fast lane: you are the only agent on this change — no plan, no reviewer agent; the review .claude/rules/github.md asks for is yours.\n\n` +
          `Implement issue #${slice.issue} "${slice.title}" on branch loop/${slice.issue}-${slice.slug}.\n\n${slice.body}${META}`,
        {
          agentType: 'coder', isolation: 'worktree', phase: 'Implement',
          label: `fast:#${slice.issue}`, schema: PR_SCHEMA, model: 'opus', effort: 'high',
        },
      )
    }
    // A proposal the owner approved is the plan; the coder then runs on Opus.
    const plan = slice.plan ?? await agent(
      `Plan the slice for issue #${slice.issue} "${slice.title}" (branch loop/${slice.issue}-${slice.slug}).\n\n${slice.body}`,
      { agentType: 'architect', phase: 'Plan', label: `plan:#${slice.issue}` },
    )
    if (!plan) throw new Error(`slice #${slice.issue}: no plan`)
    const prompt =
      `Implement issue #${slice.issue} "${slice.title}" on branch loop/${slice.issue}-${slice.slug}.\n\n${slice.body}\n\n` +
      `${slice.plan ? 'The proposal the owner approved' : "The architect's plan"}:\n\n${plan}${META}`
    return agent(prompt, {
      agentType: 'coder', isolation: 'worktree', phase: 'Implement',
      label: `code:#${slice.issue}`, schema: PR_SCHEMA,
      ...(slice.plan ? { model: 'opus', effort: 'high' } : {}),
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
    return reviewSideBySide(pr).then(async ([review, security]) => {
      const reviewed = review && (security || !pr.security)
      const fixed = [review?.fixed, security?.fixed].filter(Boolean).join(' ') || null
      if (reviewed) await closeOut(pr, fixed)
      return {
        issue: slice.issue,
        pr: pr.pr,
        branch: pr.branch,
        summary: pr.summary,
        fixed,
        uncertainty: [review?.uncertainty, security?.uncertainty].filter(Boolean).join(' ') || null,
        status: reviewed ? 'reviewed' : 'review-failed',
      }
    })
  },
)

// The review, the security review and the screenshots run side by side on one branch.
function reviewSideBySide(pr) {
  return parallel([
    () => agent(`Review pull request #${pr.pr} and fix what you find.${META}`, {
      agentType: 'reviewer', isolation: 'worktree', phase: 'Review',
      label: `review:#${pr.pr}`, schema: REVIEW_SCHEMA,
    }),
    () => pr.security
      ? agent(
        `Security review only: run /security-review on the diff of pull request #${pr.pr} and fix what it finds.${META}`,
        {
          agentType: 'reviewer', isolation: 'worktree', phase: 'Review',
          label: `security:#${pr.pr}`, schema: REVIEW_SCHEMA,
        },
      )
      : null,
    () => pr.screens
      ? agent(
        `Take the screenshots of pull request #${pr.pr} (branch ${pr.branch}) per docs/loop/visual-proof.md ` +
          `and file them in its body; read the body again right before writing it. Push no code.\n\n` +
          `Targets: ${pr.screens}${META}`,
        { isolation: 'worktree', phase: 'Review', label: `screens:#${pr.pr}`, model: 'sonnet', effort: 'medium' },
      )
      : null,
  ])
}

function closeOut(pr, fixed) {
  return agent(
    `Close out pull request #${pr.pr}: comment .github/code_review_template.md recording what the review ` +
      `fixed: ${fixed ?? 'nothing'}. Then switch agent-in-progress to agent-done. Change nothing else.`,
    { phase: 'Close', label: `close:#${pr.pr}`, model: 'sonnet', effort: 'low' },
  )
}

return slices.map((slice, i) => results[i] ?? { issue: slice.issue, status: 'failed' })
