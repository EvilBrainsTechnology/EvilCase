export const meta = {
  name: 'pr-work',
  description: 'Work a commented pull request: the coder works its branch and the reviewer fixes the rework; a fast item is the coder alone',
  phases: [
    { title: 'Work', detail: 'coder works on the existing branch' },
    { title: 'Review', detail: 'reviewer reviews the rework and fixes' },
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

const META =
  '\n\nA hook blocks edits under .claude/** and docs/sdd/**. Only where an owner comment on ' +
  'this pull request explicitly asks for such a change: run `touch .claude/allow-meta-edits`, ' +
  'edit, then delete the flag. Otherwise open an issue for the owner instead.'

const results = await pipeline(
  args,
  async (item) => {
    if (item.fast) {
      return agent(
        `Fast lane: you are the only agent on this change — no plan, no review.\n\n` +
          `Work on the existing pull request #${item.pr}, branch ${item.branch}.\n\n${item.instructions}${META}`,
        {
          agentType: 'coder', isolation: 'worktree', phase: 'Work',
          label: `fast:#${item.pr}`, schema: WORK_SCHEMA, model: 'opus', effort: 'xhigh',
        },
      )
    }
    return agent(
      `Work on the existing pull request #${item.pr}, branch ${item.branch}.\n\n${item.instructions}${META}`,
      {
        agentType: 'coder', isolation: 'worktree', phase: 'Work',
        label: `work:#${item.pr}`, schema: WORK_SCHEMA,
      },
    )
  },
  (work, item) => {
    if (!work) throw new Error(`pull request #${item.pr}: work failed`)
    if (item.fast) return { pr: item.pr, fixed: work.fixed, uncertainty: null, status: 'fast' }
    return agent(
      `Review the rework just pushed to pull request #${item.pr}, branch ${item.branch}: ` +
        `${work.fixed}\n\nFix what you find. The rest of the pull request is not yours to ` +
        `review.${META}`,
      {
        agentType: 'reviewer', isolation: 'worktree', phase: 'Review',
        label: `review:#${item.pr}`, schema: REVIEW_SCHEMA,
      },
    ).then((review) => ({
      pr: item.pr,
      fixed: work.fixed,
      uncertainty: review?.uncertainty || null,
      status: review ? 'reviewed' : 'review-failed',
    }))
  },
)

return args.map((item, i) => results[i] ?? { pr: item.pr, status: 'failed' })
