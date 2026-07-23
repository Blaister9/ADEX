# ADR-0001 — Modular monolith over microservices

- Status: Accepted
- Date: 2026-07-22
- Deciders: Foundation architect agent (task 001)

## Context

ADEX must serve online decisions with low latency, ingest events idempotently,
attribute rewards, and expose analytics — for many tenants, from day one. The
product has no traffic yet, no operations team, and no measured bottleneck. The
permanent rules in `AGENTS.md` forbid introducing distributed infrastructure
without a benchmark or ADR that demonstrates the need.

The functional areas that could eventually diverge in load profile are:

- the online decision path (latency-sensitive, read-mostly, high QPS),
- event ingestion (write-heavy, bursty, tolerant of small delays),
- analytics and reporting (scan-heavy, latency-tolerant),
- configuration and administration (low volume).

## Decision

Build a **modular monolith**: one ASP.NET Core deployable (`Adex.Api`) composed
of internally separated modules, with the module seams designed so that any of
the four areas above can later be extracted into its own process without
redesigning the domain.

Seams enforced from the start:

1. **Layer boundaries** — `Adex.Domain` (no I/O, no framework), `Adex.Application`
   (use cases and ports), `Adex.Infrastructure` (adapters), `Adex.Api`
   (composition root and transport). Dependencies point inwards only.
2. **Module boundaries inside each layer** — `Decisioning`, `Ingestion`,
   `Attribution`, `Analytics`, `Tenancy`. Cross-module calls go through
   application-level ports, never through another module's internals.
3. **No shared mutable in-process state** between modules other than the
   database and cache adapters.
4. **Contract-first HTTP surface** so extraction changes deployment topology,
   not the public API.

## Alternatives considered

- **Microservices from day one.** Rejected: multiplies deployment, tracing,
  schema-ownership and local-development cost before any load evidence exists.
  It also makes the "clone and run" success condition in `PROJECT_BRIEF.md`
  substantially harder.
- **Single-project monolith (no layer separation).** Rejected: the domain would
  entangle with EF/ASP.NET types and policy logic would stop being purely
  testable, which is a stated quality attribute.
- **Serverless functions per endpoint.** Rejected: cold-start latency conflicts
  with the low-decision-latency attribute and complicates the Thompson Sampling
  state that later policies require.

## Consequences

- One `dotnet run`, one container, one connection pool. Local development and CI
  stay cheap.
- Module boundaries must be enforced by review and by architecture tests; the
  compiler only enforces the layer boundaries (project references).
- Extraction later requires: a network hop, an inter-module contract promoted to
  a versioned API, and its own observability. The seams above are what make that
  a scoped project rather than a rewrite.
- Any future proposal to split a module must cite a measured bottleneck
  (latency percentile, saturation, or deploy-coupling incident) in a new ADR.
