# ADR-0011 — Observability baseline

- Status: Accepted
- Date: 2026-07-22
- Deciders: Foundation architect agent (task 001)

## Context

ADEX makes automated choices on someone else's website. When a tenant asks "why
did my visitor see variant B at 14:03?", the answer must come from stored data,
not from reasoning about the code. Operability through logs, metrics, traces,
health checks and runbooks is a stated quality attribute, and `AGENTS.md`
requires OpenTelemetry-compatible signals from the beginning.

## Decision

**OpenTelemetry for all three signals**, wired in `Adex.Api` at startup.

- **Traces** — ASP.NET Core instrumentation plus a custom `ActivitySource`
  (`Adex.Decisioning`) around policy evaluation and persistence. Every decision
  span carries `adex.tenant_id`, `adex.placement`, `adex.policy_key`,
  `adex.policy_version`, `adex.decision_id`. Never a subject identifier or any
  context value that could be identifying.
- **Metrics** — a `Meter` named `Adex` exposing at minimum:
  `adex.decisions.count{tenant,placement,policy,outcome}`,
  `adex.decision.duration` (histogram),
  `adex.events.count{tenant,type,result=accepted|duplicate|rejected}`,
  `adex.cache.degraded` (counter, incremented when Redis is bypassed).
- **Logs** — structured, one event per request with correlation id, tenant,
  route, status, duration. Log levels are configuration, not code edits.
- **Export** — OTLP, endpoint from `OTEL_EXPORTER_OTLP_ENDPOINT`. When the
  variable is empty, instrumentation stays in-process with no exporter; the
  application must start and behave identically with no collector present.
- **Correlation** — `X-Correlation-Id` is accepted from the client (validated,
  bounded length) or generated, echoed on every response, attached to logs and
  set as a span attribute. It is the identifier a tenant quotes in a support
  request.
- **Health** — `/health/live` (process is up; never touches dependencies) and
  `/health/ready` (PostgreSQL required, Redis optional-degraded). Readiness
  returns `200` with `status: degraded` when only optional dependencies are
  down, so a Redis outage never drains the service.

**Cardinality rule.** Metric and span attributes are restricted to bounded-
cardinality values. `tenant_id`, `placement` and `policy` are bounded by
configuration; `decision_id`, `subject_id` and free-form context values are
never used as metric dimensions.

**Audit vs telemetry.** Telemetry is sampled and expendable. The decision audit
trail lives in PostgreSQL and is never sampled — the two must not be confused,
and "why did this happen" is answered from the database.

## Alternatives considered

- **Vendor SDK (Application Insights, Datadog agent) directly.** Rejected:
  couples the code to a backend before one is chosen. OTLP keeps the backend a
  deployment decision.
- **Logs only, add tracing later.** Rejected: retrofitting trace context through
  an async request path is significantly more expensive than starting with it,
  and latency attribution across policy vs persistence is needed from the first
  latency question.
- **Prometheus scrape endpoint instead of OTLP.** Not rejected permanently; the
  OpenTelemetry Collector can expose Prometheus. Keeping the app OTLP-only
  avoids two metric pipelines in the application.

## Consequences

- Every new code path in the request pipeline is expected to add its span
  attributes and metric dimensions deliberately, subject to the cardinality
  rule.
- Local development can run without any collector; a collector service is added
  to the compose file only when someone needs to view traces locally.
- Dashboards, alerts and runbooks are downstream work items tracked in the
  roadmap, not implied by this ADR.
