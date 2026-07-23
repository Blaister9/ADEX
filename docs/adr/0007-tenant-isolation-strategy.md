# ADR-0007 — Tenant isolation strategy

- Status: Accepted
- Date: 2026-07-22
- Deciders: Foundation architect agent (task 001)

## Context

ADEX is multi-tenant from the first release. A cross-tenant leak — a decision
served from another tenant's alternatives, an event attributed to the wrong
tenant, an analytics row visible to the wrong operator — is the most damaging
class of defect this product can have. `AGENTS.md` requires isolation enforced
in both the application and the persistence layer, covered by automated tests.

## Decision

**Shared database, shared schema, mandatory `tenant_id`, defence in depth.**

1. **Transport** — every public request carries a tenant-scoped credential.
   Browser traffic uses a *publishable* key in `X-Adex-Api-Key`, bound to the
   tenant and to an allow-list of origins. Server-to-server and dashboard
   traffic use a secret key. A publishable key can only request decisions and
   emit events; it can never read analytics or configuration.
2. **Application** — the resolved `TenantId` is established once per request by
   middleware and flows explicitly as part of the use-case input. No use case
   accepts a repository call without a tenant. There is no ambient/static
   "current tenant" that a background task could inherit incorrectly.
3. **Persistence** — every tenant-owned table has a non-null `tenant_id` column,
   every primary key and unique constraint is composite on `tenant_id`, and
   every foreign key is composite so a child row cannot point at another
   tenant's parent.
4. **Database-enforced backstop** — PostgreSQL Row Level Security is enabled and
   `FORCE`d on tenant-owned tables. The application connects as a non-owner role
   and sets `app.tenant_id` per transaction; policies compare `tenant_id` to
   that setting. A query that forgets its `WHERE tenant_id = ...` returns zero
   rows instead of another tenant's data.
5. **Tests** — the isolation test suite is a required part of CI: a request
   authenticated for tenant A must never observe an object of tenant B, for
   every public endpoint, including the "not found vs forbidden" behaviour
   (ADEX returns `404` for objects outside the caller's tenant, so existence is
   not leaked).

## Alternatives considered

- **Database per tenant.** Strongest isolation, rejected for the first release:
  migration fan-out, connection-pool multiplication, and cross-tenant analytics
  cost grow with tenant count, which is exactly the growth ADEX wants to be
  cheap.
- **Schema per tenant.** Rejected for similar reasons at lower benefit;
  PostgreSQL catalog pressure grows with tenant count.
- **Application-level filtering only.** Rejected: a single forgotten predicate
  becomes a cross-tenant leak. RLS makes the default outcome empty rather than
  wrong.
- **RLS only, without composite keys.** Rejected: composite uniqueness is what
  makes `event_id` deduplication correct *per tenant* and prevents cross-tenant
  key collisions from being possible in the first place.

## Consequences

- Every migration must add `tenant_id`, the composite constraints, the RLS
  policy, and the grant — a migration checklist is part of the persistence task.
- Connection handling must set `app.tenant_id` inside the same transaction as
  the query; a pooled connection that leaks the setting is a bug class to test
  for explicitly.
- Per-tenant physical isolation remains available later as a premium tier
  without changing the domain model, because the tenant is already an explicit
  part of every key.
