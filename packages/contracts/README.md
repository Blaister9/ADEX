# @adex/contracts

The source of truth for the ADEX public HTTP surface. The ASP.NET Core API and
the browser SDK are both validated against what is in this directory; nothing
here is generated from the implementation (ADR-0006).

## Layout

| Path | Contents |
| --- | --- |
| `openapi/adex-public-v1.yaml` | OpenAPI 3.1 document for the v1 surface |
| `schemas/*.schema.json` | JSON Schema 2020-12 definitions referenced by the OpenAPI document |
| `examples/*.json` | Payloads that MUST validate — two unrelated reference tenants |
| `examples/invalid/*.json` | Payloads that MUST be rejected — proof the schemas constrain |
| `src/index.ts` | Hand-maintained TypeScript projection: types, path/header constants, problem types |
| `test/` | Schema and OpenAPI validation suites |

## Running the validation

From the repository root:

```bash
pnpm run contracts:validate
```

That runs the `contracts` Vitest project, which:

1. compiles every schema under Ajv strict mode (2020-12 + formats),
2. validates every file in `examples/` against the schema its filename names,
3. asserts every file in `examples/invalid/` is rejected,
4. dereferences the OpenAPI document, proving every `$ref` and every
   `externalValue` example file resolves,
5. asserts the document and `src/index.ts` agree on paths, the API-key header
   and the documented failure modes.

The .NET contract suite additionally submits every applicable invalid request
example to the hosted application, so an example rejected by Ajv but accepted
at runtime fails CI.

## Conventions worth knowing before editing

**Requests are closed, responses are open.** Request schemas set
`additionalProperties: false` so an integration mistake surfaces as a `422`
instead of being silently ignored. Response schemas deliberately do not, because
ADEX may add response fields within a major version and clients must ignore
unknown ones.

**Timestamps are UTC-only.** `2026-07-22T14:03:11Z` is valid;
`2026-07-22T09:03:11-05:00` is rejected rather than converted, so no ambiguity
about which clock a stored value came from can ever arise.

**Identifiers are opaque prefixed ULIDs.** `dec_`, `evt_`, `anon_`, `ten_`.
Clients must not parse them. Internal database keys are never exposed.

**Errors are RFC 9457 problem details** with two ADEX extensions:
`correlation_id` and `errors[]` (JSON Pointer + machine-readable code). Clients
branch on `type`, never on `title`.

**Two reference tenants, always.** Every example set covers a
professional-services site and an online catalog. If a proposed contract change
only makes sense for one of them, it belongs in tenant configuration, not in the
contract.

## Changing the contract

Backwards-compatible inside `v1` (see the table in ADR-0006): adding an optional
request field, adding a response field, adding an enum value. Everything else —
removing or renaming a field, changing a type, tightening validation — needs a
new major version path and a migration plan.

Checklist for any change:

1. Edit the JSON Schema and the OpenAPI document together.
2. Add or update an example for **both** reference tenants.
3. Add an invalid example if the change introduces a new constraint.
4. Update `src/index.ts` if a path, header or problem type changed.
5. Run `pnpm run contracts:validate`.
6. Run the .NET contract tests (`dotnet test tests/contract`), which assert the
   running API still matches these semantics.
