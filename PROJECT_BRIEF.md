# PROJECT_BRIEF.md

## Working name

ADEX — Adaptive Decision Engine. This is a temporary codename and may change without changing the architecture.

## Product statement

ADEX is an installable decision and experimentation layer for websites and web applications. A client integrates a small TypeScript SDK, configures placements and alternatives, and chooses a business objective. ADEX selects what content, service, message, product, ordering, or call to action to show; records the decision; receives later behavioral events; attributes configurable rewards; and reports policy performance.

## Core interaction

```text
Website/SDK
  -> request decision(context, placement, eligible alternatives)
ADEX
  -> return selected alternative + decision_id + metadata
Website/SDK
  -> emit impression/click/lead/booking/purchase/custom event
ADEX
  -> validate, deduplicate, attribute reward, update policy state, expose analytics
```

## Initial users

- A developer integrating the SDK.
- A business administrator configuring tenants, placements, alternatives, objectives, and constraints.
- An analyst reviewing conversion, uncertainty, exploration/exploitation, and policy performance.
- An operator auditing decisions, failures, latency, and data quality.

## MVP capabilities

1. Multi-tenant configuration.
2. Framework-agnostic browser SDK.
3. Versioned decision and event APIs.
4. Deterministic rule policy.
5. Uniform random/A-B policy.
6. Thompson Sampling policy for a Bernoulli objective.
7. Decision audit trail.
8. Idempotent event ingestion.
9. Configurable event-to-reward mapping.
10. Local Docker environment with PostgreSQL and Redis.
11. Synthetic simulator with reproducible scenarios.
12. Minimal dashboard for configuration and policy/result inspection.
13. CI with build, lint, tests, dependency scan, and secret scan.
14. One reference integration that demonstrates the complete path without depending on a real client website.

## Explicit non-goals for the first release

- Deep reinforcement learning.
- Cross-tenant learning.
- Automatic collection of sensitive or directly identifying data.
- Arbitrary tenant-provided executable code.
- Kubernetes, service mesh, Kafka, ClickHouse, or a microservice fleet.
- Claims of conversion uplift based solely on synthetic traffic.
- A domain-specific odontological product embedded in the core.

## Primary quality attributes

- Correctness and explainability of each decision.
- Tenant isolation.
- Low decision latency under realistic early-stage load.
- Safe degradation if Redis is unavailable.
- Reproducible policies and simulations.
- Simple integration and removal.
- Auditable configuration and policy versions.
- Privacy by default.
- Operability through logs, metrics, traces, health checks, and runbooks.

## First commercial proof

The odontological website is a reference tenant, not the architecture. Its configuration may contain dental services, messages, and conversion events, while the same core must run an unrelated reference tenant without source-code changes.

## Success condition for the foundation phase

A new developer or coding agent can clone the repository, read the durable instructions, start the local dependencies, build every component, run the test suite, understand the system boundaries, and know the next task without relying on prior chat history.
