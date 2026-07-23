# ADR-0004 — PostgreSQL as system of record, Redis as optional acceleration

- Status: Accepted
- Date: 2026-07-22
- Deciders: Foundation architect agent (task 001)

## Context

ADEX must audit every decision, deduplicate events, attribute rewards, and
report policy performance. It also must answer decisions quickly and must not
fail when a cache is unavailable. `AGENTS.md` fixes PostgreSQL as the system of
record and allows Redis for caching, rate control, and low-latency transient
state.

## Decision

**PostgreSQL is the only system of record.** Decisions, events, rewards, tenant
configuration, policy versions, and attribution rules are durable there. Any
value that cannot be reconstructed from PostgreSQL is not allowed to exist only
in Redis.

**Redis is optional and degradable.** Its permitted uses are:

1. tenant/placement configuration cache (read-through, short TTL),
2. rate limiting and abuse counters,
3. event idempotency *fast-path* (a probabilistic pre-check in front of the
   authoritative uniqueness constraint in PostgreSQL),
4. hot policy state (for example Beta posterior counters) as a cache in front of
   durable counters.

**Degradation contract.** If Redis is unreachable, the API must:

- keep serving decisions and ingesting events,
- fall back to PostgreSQL for anything Redis was accelerating,
- report `degraded` on the readiness endpoint while staying `live`,
- emit a metric and a structured log, not an exception per request.

Redis is therefore never on the correctness path: PostgreSQL unique constraints
decide duplicates, and PostgreSQL rows decide what a policy learned.

## Alternatives considered

- **Redis as primary store for policy state.** Rejected: policy state is
  auditable business data; losing it on eviction or restart would break
  reproducibility, which is a stated quality attribute.
- **PostgreSQL only, no Redis.** Viable and simpler. Kept as the actual default
  for the foundation: the compose file ships Redis, but the API must run and
  pass its tests with Redis absent. Redis becomes load-bearing only where a
  measurement justifies it.
- **A dedicated analytical store (ClickHouse and similar).** Rejected for the
  first release by `PROJECT_BRIEF.md`. Revisit only with a measured query
  bottleneck on real volumes.

## Consequences

- Every Redis-backed code path needs a PostgreSQL fallback path, and both need
  tests. The failure test matrix explicitly includes "Redis unavailable".
- Analytics queries run against PostgreSQL; index and rollup design becomes a
  real task once event volume is known (tracked in the roadmap).
- The readiness probe distinguishes *required* dependencies (PostgreSQL) from
  *optional* ones (Redis). A Redis outage must never remove the instance from
  the load balancer.
