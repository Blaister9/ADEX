# ADR-0009 — Initial policy sequence

- Status: Accepted
- Date: 2026-07-22
- Deciders: Foundation architect agent (task 001)

## Context

The product's value is adaptive selection, so the temptation is to start with
the most capable algorithm. But an adaptive policy is only as trustworthy as the
data pipeline underneath it: if attribution is wrong, a bandit will optimize
confidently toward the wrong thing and the failure is invisible. `AGENTS.md`
fixes the order: deterministic rules, uniform random / A-B, Thompson Sampling,
and only then contextual methods.

## Decision

Policies ship in this order, each gated by evidence that the previous stage is
correct.

**Stage 1 — `deterministic-rules`.** Pure function of context and alternatives:
first matching rule wins, otherwise a configured default. No randomness, no
state. Purpose: prove the decision path, the audit trail and the tenant
configuration are correct, with an outcome that is trivially explainable.

Gate to stage 2: a decision is persisted with full audit metadata and can be
replayed to the identical outcome from stored inputs.

**Stage 2 — `uniform-random` / `ab-split`.** Selection from eligible
alternatives with fixed weights (uniform by default). Randomness is seeded from
`hash(decision_seed_salt, tenant_id, placement, subject_id or decision_id)` so a
decision is reproducible from its stored inputs. Purpose: prove the reward
pipeline — impressions, events, deduplication, attribution windows — using an
unbiased assignment where the analysis is textbook.

Gate to stage 3: measured event→reward attribution matches a synthetic ground
truth in the simulator, and observed allocation matches configured weights
within tolerance on real traffic.

**Stage 3 — `thompson-sampling` (Bernoulli).** Beta-Bernoulli posterior per
(tenant, placement, alternative, policy version). Sample once per decision,
select the argmax. Posterior counters are durable in PostgreSQL; Redis may cache
them. Includes: prior configuration, a minimum-exploration floor, and reset
semantics on configuration change (a changed alternative set or reward
definition starts a new policy version rather than reusing a stale posterior).

Gate to stage 4: convergence and regret behaviour verified against seeded
synthetic environments with known optima, plus a replay evaluation on logged
production decisions.

**Stage 4 — contextual methods** (linear/logistic contextual bandits, and only
then anything heavier). Requires: stage 3 in production, off-policy evaluation
infrastructure with logged propensities, and a documented uplift hypothesis.
**Propensity logging starts at stage 2**, because off-policy evaluation is
impossible to add retroactively to unlogged history.

**Cross-cutting invariants for every policy**

- Selection is a pure function of `(context, eligible alternatives, policy
  configuration, policy state snapshot, random draw)` — all five are persisted
  with the decision.
- Every decision records `policy.key`, `policy.version`, and the propensity of
  the chosen alternative.
- A policy's configuration change produces a new `policy.version`; state is
  never silently carried across versions.
- Policies live in `Adex.Domain` with no I/O, so they are unit-testable and
  deterministic under a seeded RNG abstraction.

## Alternatives considered

- **Start at Thompson Sampling.** Rejected: it would be optimizing against an
  unverified reward signal, and a bandit converging on a bug looks exactly like
  a bandit working.
- **Epsilon-greedy instead of Thompson Sampling.** Reasonable and simpler, but
  it wastes exploration uniformly and needs a tuned epsilon; Beta-Bernoulli
  Thompson Sampling is comparably simple to implement and self-tunes. May still
  be added later as a configurable option.
- **Deep reinforcement learning.** Explicitly a non-goal for the first release
  in `PROJECT_BRIEF.md`.

## Consequences

- The first vertical slice (task 004) ships `uniform-random` only, which is a
  deliberate under-delivery of the product promise in exchange for a verifiable
  pipeline.
- Propensity and policy-state snapshots must be in the decision schema from the
  start, even while the only policy is uniform random.
- Marketing/reporting must never present synthetic simulator convergence as
  evidence of commercial uplift (`AGENTS.md` invariant 10).
