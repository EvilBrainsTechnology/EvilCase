export const meta = {
  name: 'pr-work',
  description: 'Work a commented pull request: architect plans, coder works its branch, reviewer fixes; a fast item is the coder alone',
  phases: [
    { title: 'Plan', detail: 'architect plans the rework' },
    { title: 'Work', detail: 'coder works on the existing branch' },
    { title: 'Review', detail: 'reviewer reviews and fixes' },
  ],
}

const WORK_SCHEMA = {
  type: 'object',
  properties: {
    fixed: { type: 'string', description: 'One sentence on what changed' },
    replies: { type: 'integer', description: 'Threads answered' },
  },
  required: ['fixed'],
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
        `Fast lane: you are the only agent on this change — no plan, no review.\n\n` +
          `Work on the existing pull request #${item.pr}, branch ${item.branch}.\n\n${item.instructions}`,
        {
          agentType: 'coder', isolation: 'worktree', phase: 'Work',
          label: `fast:#${item.pr}`, schema: WORK_SCHEMA, model: 'opus', effort: 'xhigh',
        },
      )
    }
    const prompt =
      `Work on the existing pull request #${item.pr}, branch ${item.branch}.\n\n${item.instructions}\n\n` +
      `The architect's plan:\n\n${plan}`
    return agent(prompt, {
      agentType: 'coder', isolation: 'worktree', phase: 'Work',
      label: `work:#${item.pr}`, schema: WORK_SCHEMA,
    })
  },
  (work, item) => {
    if (!work) throw new Error(`pull request #${item.pr}: work failed`)
    if (item.fast) return { pr: item.pr, fixed: work.fixed, uncertainty: null, status: 'fast' }
    return agent(`Review pull request #${item.pr} and fix what you find.`, {
      agentType: 'reviewer', isolation: 'worktree', phase: 'Review',
      label: `review:#${item.pr}`, schema: REVIEW_SCHEMA,
    }).then((review) => ({
      pr: item.pr,
      fixed: work.fixed,
      uncertainty: review?.uncertainty || null,
      status: review ? 'reviewed' : 'review-failed',
    }))
  },
)

return args.map((item, i) => results[i] ?? { pr: item.pr, status: 'failed' })
