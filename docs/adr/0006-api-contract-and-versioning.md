# ADR-0006 — Contract-first API and versioning strategy

- Status: Accepted
- Date: 2026-07-22
- Deciders: Foundation architect agent (task 001)

## Context

The public HTTP surface is consumed by a browser SDK embedded in third-party
websites. Those pages update on the tenant's schedule, not on ours: a deployed
SDK version can stay in production for months. The contract therefore has to be
stable, explicit, and independently verifiable.

## Decision

**Contract-first.** `packages/contracts/openapi/adex-public-v1.yaml` and the
JSON Schemas beside it are authored by hand and are the source of truth. The API
implementation is tested against them; the contract is not generated from the
implementation.

**URL-path major versioning.** `/v1/...`. A new major version is a new path,
served side by side with the previous one for a documented deprecation window.

**Compatibility rules inside a major version:**

| Change | Allowed in v1? |
| --- | --- |
| Add an optional request field | Yes |
| Add a response field | Yes — clients must ignore unknown fields |
| Add a new enum value to a *response* | Yes, with a documented default handling |
| Add a new enum value to a *request* | Yes |
| Make an optional request field required | No |
| Remove or rename any field | No |
| Change a field's type or format | No |
| Tighten a validation rule so previously accepted payloads fail | No |
| Change the meaning of an existing value | No |

**Naming and encoding conventions**

- JSON bodies use `snake_case`. Headers use `X-Adex-*`.
- All timestamps are RFC 3339 with an explicit `Z` UTC offset. The API rejects
  non-UTC offsets rather than converting silently.
- All public identifiers are opaque prefixed strings: `dec_`, `evt_`, `anon_`,
  `ten_`. Clients must treat them as opaque; internal surrogate keys are never
  exposed.
- Errors use RFC 9457 `application/problem+json` with ADEX extensions
  `correlation_id` and `errors[]`.

**Internal entities are never exposed.** Public DTOs are declared in the
contract package and mapped explicitly; no database entity is serialized
directly.

**Event schema versioning.** Ingested event payloads carry the contract version
implicitly through the endpoint path. A backward-incompatible event schema
change requires a migration plan (`AGENTS.md`) and a new endpoint version.

## Alternatives considered

- **Header or query-parameter versioning.** Rejected: harder to cache, harder to
  route, harder to see in logs and in a browser network panel — and the SDK is
  debugged in browser network panels.
- **Generate OpenAPI from ASP.NET Core at build time.** Rejected as the source of
  truth: the contract would then follow implementation drift instead of
  constraining it. Generated output may still be produced later as a *check*
  against the hand-written contract.
- **GraphQL.** Rejected: the public surface is two write-shaped operations with a
  strict latency budget; GraphQL's flexibility is a liability on a public,
  origin-restricted, browser-facing endpoint.
- **gRPC.** Rejected for the public surface (browser support cost); may be
  reconsidered for internal traffic if a module is ever extracted.

## Consequences

- Contract changes are reviewable diffs in one directory.
- CI validates the OpenAPI document and validates every example against the
  schemas; the .NET contract tests assert the running API matches the same
  semantics.
- A deliberate cost: the contract and the C# DTOs are maintained in parallel.
  Drift is caught by the contract tests, which is why those tests are required
  rather than optional.
