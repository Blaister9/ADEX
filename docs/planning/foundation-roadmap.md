# Foundation roadmap

Sequenced work items from the end of task 001 to a production-capable first
release. Each item names its deliverable and the evidence that closes it. The
agent sequence in `MULTI_AGENT_ORCHESTRATION.md` maps onto these items.

Status legend: `done` · `next` · `planned`

## 001 — Repository foundation (done)

Structure, ADRs, architecture documentation, contracts, buildable shells, local
infrastructure, CI, handoff. Evidence: this repository and
`docs/handoffs/current.md`.

## 002 — Adversarial architecture review (next)

Independent agent, read-only except for `docs/reviews/001-foundation-review.md`.
Must re-run every documented command, challenge the ADRs, and rank findings by
severity. Evidence: the review document plus reproduced command output.

## 003 — Foundation corrections (planned)

Fix accepted findings from 002 on its own branch; close each finding with
evidence. Evidence: updated review document with per-finding resolution.

## 004 — First vertical slice (planned, highest value)

The single most important item. Deliverables:

- PostgreSQL schema migrations for tenants, API keys, placements, alternatives,
  policy bindings, decisions, events — with `tenant_id` on every table,
  composite keys, and RLS enabled and `FORCE`d (ADR-0007).
- Real persistence adapters replacing the development in-memory stores in
  `Adex.Infrastructure`, selected by `Adex:Persistence:Provider=Postgres`.
- Tenant resolution from a publishable API key with an origin allow-list.
- End-to-end path: reference page → `POST /v1/decisions` (uniform random) →
  persisted decision → `POST /v1/events` → attributed Bernoulli reward → one
  analytics query.
- Reference integration page under `tests/e2e/` that works with the SDK disabled.
- Integration tests against real containers; isolation tests as a required CI
  job.

Evidence: integration + e2e suites green against `docker compose up`, and a
replay test proving a stored decision reproduces its selected alternative.

## 005 — Policy engine and simulator (planned)

Deterministic rules, A/B weights, Thompson Sampling (Beta-Bernoulli), durable
policy state with version isolation, propensity logging, window closure job.
Python simulator gains seeded scenarios, offline evaluation, regret and
conversion analysis, and cross-language fixture equivalence tests (ADR-0003).
All synthetic output labelled as synthetic.

Evidence: convergence and regret tests against known synthetic optima; C#/Python
fixture equivalence; replay evaluation on logged decisions.

## 006 — Configuration and administration dashboard (planned)

Tenant, placement, alternative, policy binding, constraint and attribution-rule
management through the public API only. Analytics screens for conversion,
uncertainty and exploration/exploitation. Dashboard authentication and secret
key handling designed and reviewed.

Evidence: dashboard tests against a running API; no direct database access from
the dashboard.

## 007 — Security, reliability and release readiness (planned)

Isolation test expansion, abuse-case tests, failure injection (Redis down,
PostgreSQL failover, slow dependencies), rate limiting, signed decision tokens,
deletion by subject identifier, load baseline validating or correcting the
latency budget, backup/restore rehearsal, runbooks.

Evidence: measured load report, failure-injection results, restore rehearsal
record.

## Cross-cutting debts opened by task 001

These are known and deliberate; each must be closed by the item named.

| Debt | Closed by |
| --- | --- |
| In-memory development stores are the only persistence | 004 |
| No PostgreSQL entity schema, no RLS in place | 004 |
| Tenant resolution accepts development keys from configuration | 004 |
| No rate limiting or origin allow-list enforcement | 004 / 007 |
| Latency budget is a target, never measured | 007 |
| SDK has no storage/consent layer and never persists a subject id | 004 |
| Dashboard is a shell with no configuration screens | 006 |
| Simulator has an environment generator but no policy evaluation harness | 005 |
| Docker Compose was never started in the bootstrap environment | 002 (verify) |
| No `CODEOWNERS` (no real ownership information exists yet) | when owners exist |
| No `LICENSE` (licensing decision is the owner's) | human decision |

## Explicitly deferred until evidence exists

Kubernetes, Kafka, ClickHouse, a service mesh, microservice extraction, C++ or
WebAssembly components, deep reinforcement learning, and cross-tenant learning.
Each requires a benchmark or an ADR demonstrating need (`AGENTS.md`).
