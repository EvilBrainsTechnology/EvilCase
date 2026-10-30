export const meta = {
  name: 'pr-work',
  description: 'Work a commented pull request: architect plans, coder works its branch, reviewer fixes; a fast item is the coder alone',
  phases: [
    { title: 'Plan', detail: 'architect plans the rework' },
    { title: 'Work', detail: 'coder works on the existing branch' },
    { title: 'Review', detail: 'review, security review and screenshots side by side' },
    { title: 'Close', detail: 'review comment and agent-done' },
  ],
}

const WORK_SCHEMA = {
  type: 'object',
  properties: {
    fixed: { type: 'string', description: 'One sentence on what changed' },
    replies: { type: 'integer', description: 'Threads answered' },
    screens: { type: 'string', description: 'The screenshot targets where a screen changes, or empty' },
    security: { type: 'boolean', description: 'Whether authentication or file storage changed' },
  },
  required: ['fixed', 'screens', 'security'],
}

const REVIEW_SCHEMA = {
  type: 'object',
  properties: {
    fixed: { type: 'string', description: 'One sentence on what the review fixed, or "nothing"' },
    uncertainty: { type: 'string', description: 'The one sentence left for the owner, or empty' },
  },
  required: ['fixed'],
}

const SHOTS_SCHEMA = {
  type: 'object',
  properties: {
    ok: { type: 'boolean', description: 'Whether the screenshots are filed in the body' },
    error: { type: 'string', description: 'What failed, or empty' },
  },
  required: ['ok'],
}

const META =
  '\n\nA hook blocks edits under .claude/** and docs/sdd/**. Only where an owner comment on ' +
  'this pull request explicitly asks for such a change: run `touch .claude/allow-meta-edits`, ' +
  'edit, then delete the flag. Otherwise open an issue for the owner instead.'

const results = await pipeline(
  args,
  async (item) => {
    // The fast lane has no plan; the work stage ignores it. Returning null here would
    // drop the item and skip the work stage.
    if (item.fast) return 'fast lane: no plan'
    const plan = await agent(
      `Plan the rework of pull request #${item.pr} (branch ${item.branch}).\n\n${item.instructions}`,
      { agentType: 'architect', phase: 'Plan', label: `plan:#${item.pr}` },
    )
    if (!plan) throw new Error(`pull request #${item.pr}: no plan`)
    return plan
  },
  (plan, item) => {
    if (item.fast) {
      return agent(
        `Fast lane: you are the only agent on this change — no plan, no reviewer agent; the review .claude/rules/github.md asks for ` +
          `is yours, /code-review medium on the rework's diff. The screenshots where a screen changes are yours too.\n\n` +
          `Work on the existing pull request #${item.pr}, branch ${item.branch}.\n\n${item.instructions}${META}`,
        {
          agentType: 'coder', isolation: 'worktree', phase: 'Work',
          label: `fast:#${item.pr}`, schema: WORK_SCHEMA, model: 'opus', effort: 'high',
        },
      )
    }
    const prompt =
      `Work on the existing pull request #${item.pr}, branch ${item.branch}.\n\n${item.instructions}\n\n` +
      `The architect's plan:\n\n${plan}${META}`
    return agent(prompt, {
      agentType: 'coder', isolation: 'worktree', phase: 'Work',
      label: `work:#${item.pr}`, schema: WORK_SCHEMA,
    })
  },
  (work, item) => {
    if (!work) throw new Error(`pull request #${item.pr}: work failed`)
    if (item.fast) return { pr: item.pr, fixed: work.fixed, uncertainty: null, status: 'fast' }
    return parallel([
      () => agent(
        `Review the rework just pushed to pull request #${item.pr}, branch ${item.branch}: ` +
          `${work.fixed}\n\nFix what you find. The rest of the pull request is not yours to ` +
          `review.${META}`,
        {
          agentType: 'reviewer', isolation: 'worktree', phase: 'Review',
          label: `review:#${item.pr}`, schema: REVIEW_SCHEMA,
        },
      ),
      () => work.security
        ? agent(
          `Security review only: run /security-review on the rework just pushed to pull request ` +
            `#${item.pr}, branch ${item.branch}, and fix what it finds.${META}`,
          {
            agentType: 'reviewer', isolation: 'worktree', phase: 'Review',
            label: `security:#${item.pr}`, schema: REVIEW_SCHEMA,
          },
        )
        : null,
      () => screenshots(item, work),
    ]).then(async ([review, security, shots]) => {
      const reviewed = review && (security || !work.security) && (shots?.ok || !work.screens)
      const fixed = [review?.fixed, security?.fixed].filter(Boolean).join(' ') || 'nothing'
      const uncertainty = [review?.uncertainty, security?.uncertainty].filter(Boolean).join(' ') || null
      if (reviewed) {
        await agent(
          `Close out pull request #${item.pr}. Comment .github/code_review_template.md recording what the ` +
            `review of the rework fixed: ${fixed}. Where a fix made the description wrong, correct that part` +
            `${uncertainty ? `, and add this sentence for the owner to it: ${uncertainty}` : ''}. Then switch ` +
            `agent-in-progress to agent-done. Change no code.`,
          { phase: 'Close', label: `close:#${item.pr}`, model: 'sonnet', effort: 'low' },
        )
      }
      return {
        pr: item.pr,
        fixed: work.fixed,
        uncertainty,
        status: reviewed ? 'reviewed' : 'review-failed',
        screenshots: shots?.error || null,
      }
    })
  },
)

// Screenshots only where the rework changed a screen.
function screenshots(item, work) {
  if (!work.screens) return null
  return agent(
    `Take the screenshots of pull request #${item.pr} (branch ${item.branch}) per docs/loop/visual-proof.md ` +
      `and replace the Screenshots section of its body; read the body again right before writing it. ` +
      `Push no code; a page that throws is reported, not fixed.\n\nTargets: ${work.screens}${META}`,
    {
      isolation: 'worktree', phase: 'Review', label: `screens:#${item.pr}`,
      schema: SHOTS_SCHEMA, model: 'sonnet', effort: 'medium',
    },
  )
}

return args.map((item, i) => results[i] ?? { pr: item.pr, status: 'failed' })
