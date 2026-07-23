# ADR-0003 — Language boundaries: C# online path, TypeScript SDK, Python simulation

- Status: Accepted
- Date: 2026-07-22
- Deciders: Foundation architect agent (task 001)

## Context

Three very different workloads exist: a browser integration layer, a
latency-sensitive multi-tenant online service, and offline statistical
experimentation. Using one language everywhere would make one of the three
significantly worse. Using more than three would fragment the repository.

## Decision

Three languages, with a hard rule on which one owns each responsibility.

**TypeScript — integration and presentation only.**
`packages/sdk-web` is framework-agnostic (no React/Vue/Angular dependency, no
build-tool assumption, zero runtime dependencies). It collects approved context,
requests a decision, applies the returned alternative, and emits events. It
contains **no authoritative policy logic**: a tenant must not be able to change
the outcome of a decision by tampering with the browser. `apps/dashboard` is a
TypeScript application that consumes the same public API as any other client.

**C# / ASP.NET Core — the authoritative online path.**
Every decision that a client can observe is produced by `Adex.Api`. Policy
evaluation, tenant resolution, idempotency, attribution and persistence live in
`src/`. The online path must not call out to a Python process in the first
release: it would add a second runtime, a serialization hop, and a second
failure domain to the p99 latency budget.

**Python — offline only.**
`simulation/adex-simulator` generates synthetic environments, replays logged
decisions, evaluates policies offline, and produces regret/conversion analyses.
It is never in the request path. It may read exported data and may write
reports, never production state.

**Crossing the boundary.** When a policy proven in Python is promoted to
production, it is *reimplemented* in `Adex.Domain` and validated against the
Python implementation using shared, seeded fixtures. The fixtures — not the
code — are the contract.

## Alternatives considered

- **Python online service for policies.** Rejected: second runtime in the
  latency path, and the first three policies (rules, A/B, Thompson Sampling for
  a Bernoulli objective) are a few dozen lines of arithmetic that C# expresses
  precisely.
- **Node.js backend so the whole stack is TypeScript.** Rejected: `AGENTS.md`
  fixes ASP.NET Core as the authoritative backend, and the numeric/concurrency
  characteristics of the decision path favour the .NET runtime.
- **C# for simulation too.** Rejected: the experimentation ecosystem (NumPy,
  SciPy, pandas, matplotlib) has no equivalent-cost .NET counterpart, and
  simulation is where iteration speed matters most.
- **Skip Python until later.** Rejected: reproducible offline evaluation is a
  stated quality attribute, and retrofitting it after policies ship means
  shipping policies that were never evaluated.

## Consequences

- Policy logic is written twice (C# production, Python research). This is
  accepted, and mitigated by shared seeded fixtures plus an equivalence test
  that must exist before any policy beyond uniform random is promoted.
- Three toolchains must be installable for a full local verification. The README
  documents how to verify each independently so a contributor working on one
  surface does not need all three.
- The SDK cannot be trusted for anything security-relevant; the API validates
  everything it receives (see `docs/security/threat-model-initial.md`).
