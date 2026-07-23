# ADR-0012 — Idempotency and event deduplication

- Status: Accepted
- Date: 2026-07-22
- Deciders: Foundation architect agent (task 001)

## Context

Events are emitted from browsers over unreliable networks, often during page
unload. Retries, `sendBeacon` duplicates, bfcache restores, and user-triggered
double submits are normal, not exceptional. If duplicates reach the reward
counters, the policy learns from inflated evidence — a silent correctness
failure. `AGENTS.md` requires idempotent event ingestion.

## Decision

**Client-generated event identity.** The caller supplies `event_id`
(`evt_<26-char ULID>`). ADEX treats `(tenant_id, event_id)` as the uniqueness
key, enforced by a composite unique constraint in PostgreSQL — the database, not
the application, is the arbiter.

**Response semantics.** Ingestion returns `202 Accepted` with a body containing
`event_id`, `status` (`accepted` or `duplicate`), and `received_at`. A replay is
not an error: it returns `202` with `status: "duplicate"` and does not modify
state. Callers therefore need no special-case logic to retry safely.

**Deduplication window.** The uniqueness constraint is permanent for the
retention period of the events table. A Redis fast-path may answer "seen
recently" for a bounded window to avoid a database round trip, but a Redis miss
always falls through to PostgreSQL — Redis can only make it faster, never
change the answer (ADR-0004).

**Reward attribution is exactly-once by construction.** Reward counters are
updated in the same transaction that inserts the event row. Because the insert
carries the unique constraint, a duplicate insert fails and the counter update
never runs. There is no separate "have I already counted this?" bookkeeping.

**Decision idempotency.** Decision requests are *not* idempotent by default: two
identical requests are two decisions with two `decision_id`s, which is correct
because two impressions occurred. A client that needs a stable answer for a
retried request may send an `Idempotency-Key` header; the API then returns the
previously produced decision for the same `(tenant_id, idempotency_key)` for a
bounded window (24 hours), including the same `decision_id`. A key reused with a
*different* request body is rejected with `409`.

**Clock discipline.** `occurred_at` is client-supplied and untrusted. The API
records both `occurred_at` and the server-side `received_at`. Events with
`occurred_at` more than 5 minutes in the future or more than 7 days in the past
are rejected with a validation error; all analytics that depend on ordering use
`received_at` unless a query explicitly asks for client time.

## Alternatives considered

- **Server-generated event ids.** Rejected: the client cannot then retry safely,
  because a lost response is indistinguishable from a lost request.
- **Content-hash deduplication.** Rejected: two genuinely distinct clicks with
  identical payloads inside the same second would be collapsed — silent data
  loss instead of silent duplication.
- **Deduplication only in Redis.** Rejected: eviction or a restart would let
  duplicates through with no trace, and correctness would depend on an optional
  component.
- **Rejecting duplicates with `409`.** Rejected: it makes correct retry logic
  harder for SDK consumers, and every client would then need to treat one error
  code as success.

## Consequences

- The events table carries a unique index on `(tenant_id, event_id)`; its size
  and the retention/partitioning strategy must be designed together.
- The SDK must generate ULIDs client-side and persist the pending event queue
  across a page unload for at-least-once delivery.
- Load tests must include a duplicate-heavy profile, because the duplicate path
  hits a constraint violation and its cost is not the same as an insert.
- Validation of `occurred_at` skew must be covered by tests, including a client
  with a badly wrong clock.
