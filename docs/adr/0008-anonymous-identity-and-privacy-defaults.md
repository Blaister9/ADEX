# ADR-0008 — Anonymous identity and privacy defaults

- Status: Accepted
- Date: 2026-07-22
- Deciders: Foundation architect agent (task 001)

## Context

ADEX needs to link a decision to the later events it caused, which requires some
notion of a subject. It must do so without collecting personal data by default,
without third-party tracking, and without fingerprinting — `AGENTS.md` requires
first-party, rotatable, configurable anonymous identifiers and minimal context
collection.

## Decision

**Subject identifier**

- The SDK generates an opaque random identifier `anon_<26-char ULID>` using the
  platform CSPRNG. It is **not** derived from any device or browser property.
- Storage is first-party only: a first-party cookie on the tenant's own origin,
  or `localStorage`, chosen by tenant configuration. No third-party cookie, no
  cross-site storage, no `localStorage` fallback chain that silently resurrects
  a cleared identifier.
- Default lifetime is 90 days of inactivity, after which a new identifier is
  generated. Tenants may shorten it; a value above 400 days is rejected.
- The identifier is **tenant-scoped**. The same browser visiting two ADEX
  tenants produces two unrelated identifiers.
- `subject_id` is **optional** in the decision contract. With no subject, ADEX
  still returns a decision; only subject-level personalization and multi-session
  attribution are unavailable. Storage refusal degrades to a memory-only
  identifier for the page lifetime.

**Tenant-supplied identifiers**

A tenant may supply its own stable identifier (for example its logged-in user
key). ADEX never stores it raw: it stores
`HMAC-SHA256(environment_salt || tenant_id, supplied_id)`, truncated to 128
bits. The salt is environment-scoped, rotatable, and never committed
(`Adex__Privacy__SubjectSalt`). Rotating the salt breaks linkage forward, which
is the intended privacy property.

**Context minimization**

- The `context` object is an allow-list of low-cardinality, non-identifying
  keys. The foundation defines `device_class`, `referrer_group`, `locale`,
  `page_group`, `session_ordinal`.
- Free-form context keys are rejected unless declared in tenant configuration,
  and every declared key is documented in the tenant's own configuration record.
- The SDK never collects: user agent strings verbatim, screen/canvas/font
  fingerprints, precise geolocation, IP-derived identifiers, form contents, or
  page text.
- The API does not persist raw client IP addresses. If a coarse geography
  signal is ever needed it will be derived at the edge and stored as a
  low-cardinality region code, decided in its own ADR.

**Retention and deletion**

- Raw events have a default retention of 400 days; aggregates outlive them.
- Deletion by `subject_id` must be supported before any production tenant
  onboards; it is a tracked roadmap item, not an assumed capability.

## Alternatives considered

- **Device fingerprinting for a stable id.** Rejected: it is the exact opposite
  of the stated privacy posture and is illegal or restricted in several target
  markets without consent.
- **Server-generated identifier via a set-cookie from the ADEX domain.**
  Rejected: that is a third-party cookie, blocked by default in major browsers
  and hostile to the product's positioning.
- **No subject identifier at all.** Rejected: reward attribution across a
  session becomes impossible, which removes the product's core value.
- **Storing tenant-supplied identifiers raw.** Rejected: it would make ADEX a
  processor of directly identifying data by default.

## Consequences

- Attribution is per-browser and best-effort. Cross-device journeys are not
  reconstructed, and reporting must state this rather than imply user-level
  truth.
- The salt is operationally important: losing it breaks linkage for
  tenant-supplied identifiers; leaking it enables re-identification of those
  identifiers. It is handled as a secret with a documented rotation procedure.
- Consent management stays the tenant's responsibility; ADEX must expose an
  explicit "do not set storage" mode so a tenant's consent tool can drive it.
