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

const QUESTION = { type: 'string', description: 'Only where a question stops the work: the question for the owner' }

const asked = (q) => q?.trim() || null

const WORK_SCHEMA = {
  type: 'object',
  properties: {
    fixed: { type: 'string', description: 'One sentence on what changed, a stopped earlier push included' },
    replies: { type: 'integer', description: 'Threads answered' },
    screens: { type: 'string', description: 'The screenshot targets where a screen changes, or empty' },
    security: { type: 'boolean', description: 'Whether authentication or file storage changed' },
    question: QUESTION,
  },
  required: ['fixed', 'screens', 'security'],
}

const REVIEW_SCHEMA = {
  type: 'object',
  properties: {
    fixed: { type: 'string', description: 'One sentence on what the review fixed, or "nothing"' },
    question: QUESTION,
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

const LABEL = 'Then switch agent-in-progress to agent-done, or to waiting-for-agent where ci-failed is on it. '

const OPEN =
  'First `gh pr view`: a merged or closed pull request ends the run — change nothing, report it. '

const DONE_SCHEMA = {
  type: 'object',
  properties: {
    done: { type: 'boolean', description: 'Whether the comment and the label are on the pull request' },
    closed: { type: 'boolean', description: 'Whether the pull request was already merged or closed' },
  },
  required: ['done', 'closed'],
}

// A sonnet agent that did not land its comment and label gets one more try.
async function landed(run) {
  const once = await run()
  return once?.done || once?.closed ? once : run()
}

const META =
  '\n\nA hook blocks edits under .claude/** and docs/sdd/**. Only where an owner comment on ' +
  'this pull request explicitly asks for such a change: run `touch .claude/allow-meta-edits`, ' +
  'edit, then delete the flag. Otherwise, and for anything unclear, return the question for the ' +
  'owner in `question` (.claude/rules/agents.md).'

const PLAN_SCHEMA = {
  type: 'object',
  properties: {
    plan: { type: 'string', description: 'The plan, or empty where a question stops it' },
    question: QUESTION,
  },
  required: [],
}

// args may arrive as a JSON string.
const items = typeof args === 'string' ? JSON.parse(args) : args

const results = await pipeline(
  items,
  async (item) => {
    // The fast lane has no plan; the work stage ignores it. Returning null here would
    // drop the item and skip the work stage.
    if (item.fast) return 'fast lane: no plan'
    const planned = await agent(
      `Plan the rework of pull request #${item.pr} (branch ${item.branch}).\n\n${item.instructions}`,
      { agentType: 'architect', phase: 'Plan', label: `plan:#${item.pr}`, schema: PLAN_SCHEMA },
    )
    if (asked(planned?.question)) return { question: asked(planned.question) }
    if (!planned?.plan) throw new Error(`pull request #${item.pr}: no plan`)
    return planned.plan
  },
  (plan, item) => {
    if (plan?.question) return { fixed: 'nothing', question: plan.question }
    if (item.fast) {
      return agent(
        `Fast lane: you are the only agent on this change — no plan, no reviewer agent; the review .claude/rules/github.md asks for ` +
          `is yours: /code-review medium on the rework's diff, or what the instructions name as missing. The screenshots ` +
          `where a screen changes are yours too.\n\n` +
          `Work on the existing pull request #${item.pr}, branch ${item.branch}.\n\n${item.instructions}${META}`,
        {
          agentType: 'coder', isolation: 'worktree', phase: 'Work',
          label: `fast:#${item.pr}`, schema: WORK_SCHEMA, model: 'opus', effort: 'high',
        },
      )
    }
    const prompt =
      `Work on the existing pull request #${item.pr}, branch ${item.branch}.\n\n${item.instructions}\n\n` +
      `The architect's plan:\n\n${plan}\n\n` +
      `The screenshots are taken beside the review: return their targets only.${META}`
    return agent(prompt, {
      agentType: 'coder', isolation: 'worktree', phase: 'Work',
      label: `work:#${item.pr}`, schema: WORK_SCHEMA,
    })
  },
  (work, item) => {
    if (!work) throw new Error(`pull request #${item.pr}: work failed`)
    // A question stops the rework before its review; the orchestrator asks it.
    if (asked(work.question)) return { pr: item.pr, fixed: work.fixed, question: asked(work.question), status: 'question' }
    if (item.fast) return { pr: item.pr, fixed: work.fixed, question: null, status: 'fast' }
    return parallel([
      () => agent(
        `Review the rework just pushed to pull request #${item.pr}, branch ${item.branch}: ` +
          `${work.fixed}\n\nFix what you find. The rest of the pull request is not yours to ` +
          `review.${work.security ? ' The security review runs beside you, another agent\'s: skip it.' : ''}${META}`,
        {
          agentType: 'reviewer', isolation: 'worktree', phase: 'Review',
          label: `review:#${item.pr}`, schema: REVIEW_SCHEMA,
        },
      ),
      () => work.security
        ? agent(
          `Security review only: run /security-review on the rework just pushed to pull request ` +
            `#${item.pr}, branch ${item.branch}, and fix what it finds. Skip the checklist review, another ` +
            `agent runs it; the coder's check only where you pushed.${META}`,
          {
            agentType: 'reviewer', isolation: 'worktree', phase: 'Review',
            label: `security:#${item.pr}`, schema: REVIEW_SCHEMA,
          },
        )
        : null,
      () => screenshots(item, work),
    ]).then(async ([review, security, shots]) => {
      // Each item names what the next agent runs, so the hand-off comment is its instructions.
      const missing = [
        !review && 'the code review: /code-review high on the rework',
        work.security && !security && 'the security review: /security-review on the rework',
        work.screens && !shots?.ok && `the screenshots per docs/loop/visual-proof.md (${shots?.error || 'the agent died'})`,
      ].filter(Boolean).join('; ')
      const fixed = [review?.fixed, security?.fixed].filter(Boolean).join(' ') || 'nothing'
      const question = [review?.question, security?.question].map(asked).filter(Boolean).join(' ') || null
      // A failed stage hands the pull request to the next round; the comment comes before the label,
      // pr-state.yml sets agent-done from it even when the label switch is lost.
      const closed = await landed(() => missing
        ? agent(
          `${OPEN}Pull request #${item.pr} did not get its full review. Comment on it, without a heading: ` +
            `"Review of the rework not finished; missing: ${missing}." Add what the finished part fixed: ${fixed}. ` +
            `Then switch agent-in-progress to waiting-for-agent. Change nothing else.`,
          { phase: 'Close', label: `handoff:#${item.pr}`, schema: DONE_SCHEMA, model: 'sonnet', effort: 'low' },
        )
        : agent(
          `${OPEN}Close out pull request #${item.pr}. Comment .github/code_review_template.md recording what ` +
            `the review of the rework fixed: ${fixed}. Where a fix made the description wrong, correct that part. ` +
            `${LABEL}Change no code.`,
          { phase: 'Close', label: `close:#${item.pr}`, schema: DONE_SCHEMA, model: 'sonnet', effort: 'low' },
        ))
      return {
        pr: item.pr,
        fixed: work.fixed,
        question,
        status: closed?.closed ? 'closed' : missing ? 'review-failed' : closed?.done ? 'reviewed' : 'close-failed',
        missing: missing || null,
      }
    })
  },
)

// Screenshots only where the rework changed a screen.
function screenshots(item, work) {
  if (!work.screens) return null
  return agent(
    `${OPEN}Take the screenshots of pull request #${item.pr} (branch ${item.branch}) per docs/loop/visual-proof.md ` +
      `and replace the Screenshots section of its body; read the body again right before writing it. ` +
      `Push no code; a page that throws is reported, not fixed.\n\nTargets: ${work.screens}${META}`,
    {
      isolation: 'worktree', phase: 'Review', label: `screens:#${item.pr}`,
      schema: SHOTS_SCHEMA, model: 'sonnet', effort: 'medium',
    },
  )
}

return items.map((item, i) => results[i] ?? { pr: item.pr, status: 'failed' })
