# ADR-0014 — Foundation observability semantics

- Status: Accepted
- Date: 2026-07-22
- Deciders: Foundation remediation agent (task 003)

## Context

ADR-0011 required logs, metrics and traces, but the foundation wired only
traces and metrics. It also described `adex.cache.degraded` as a PostgreSQL
fallback counter even though no cache-backed application read exists before the
durable configuration adapters land.

## Decision

The API emits one structured log for every request with correlation id, tenant,
route, status and duration. Logging uses the OpenTelemetry provider and the
same optional OTLP export boundary as traces and metrics.

During the foundation, `adex.cache.degraded` counts executable readiness
observations where Redis is unavailable or not configured, with a bounded
`reason` tag. It does not claim PostgreSQL answered a cache miss. The health
detail is provider-neutral. When a real read-through cache path is introduced,
its bypass metric and fallback semantics require an adapter-level test and an
ADR update.

## Alternatives considered

- **Increment the counter on every decision while Redis is down.** Rejected:
  there is currently no Redis-backed decision read to bypass, so that would
  manufacture an operational signal.
- **Remove the counter until persistence exists.** Rejected: operators still
  need a measurable optional-dependency degradation signal in the executable
  foundation.

## Consequences

- In-memory exporter and meter-listener tests make the three signals
  executable rather than documentary.
- The counter measures degraded dependency observations, not request volume
  affected by a cache fallback.
- No subject, decision id or free-form context becomes a log/metric dimension.
