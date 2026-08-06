export const meta = {
  name: 'pr-work',
  description: 'Work a commented pull request: optional plan, coder on its branch, optional review',
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
  (item) => {
    if (!item.full) return null
    return agent(
      `Plan the rework of pull request #${item.pr} (branch ${item.branch}).\n\n${item.instructions}`,
      { agentType: 'architect', phase: 'Plan', label: `plan:#${item.pr}` },
    )
  },
  (plan, item) => {
    const prompt =
      `Work on the existing pull request #${item.pr}, branch ${item.branch}.\n\n${item.instructions}\n\n` +
      (plan ? `The architect's plan:\n\n${plan}` : 'Switch the label to `agent-done` at the end.')
    return agent(prompt, {
      agentType: 'coder', isolation: 'worktree', phase: 'Work',
      label: `work:#${item.pr}`, schema: WORK_SCHEMA,
    })
  },
  (work, item) => {
    if (!work) throw new Error(`pull request #${item.pr}: work failed`)
    if (!item.full) return { pr: item.pr, fixed: work.fixed, status: 'done' }
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
