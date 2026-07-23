# Summary

<!-- What changed and why. Link the roadmap item or review finding it closes. -->

## Verification

Paste the **actual** command output, not a claim that it passed. `AGENTS.md`:
never state that a build, test or migration succeeded unless it was executed and
its result inspected. A check that could not run belongs under "Not verified"
below, with its exact error.

```text
<!-- dotnet build / dotnet test / pnpm verify / pytest / docker compose config -->
```

## Not verified

<!-- Checks blocked by the environment: the exact command, the exact error, the
     likely cause, and the next action. Write "none" if everything ran. -->

## Architecture

- [ ] No new dependency in `Adex.Domain` (it has none by design)
- [ ] No new NuGet/npm/PyPI dependency, or it is justified below
- [ ] No microservice, Kubernetes, Kafka, ClickHouse, C++ or WebAssembly
      introduced without an ADR citing a measurement
- [ ] Any architectural decision that constrains future work has an ADR

## Multi-tenancy and security

- [ ] Every new query, use case and table takes the tenant explicitly
- [ ] New tenant-owned tables carry `tenant_id`, composite keys, RLS and grants
      (checklist in `infra/local/README.md`)
- [ ] Objects outside the caller's tenant return `404`, never `403`
- [ ] Isolation is covered by a test, not only by review
- [ ] No secret, token, credential or absolute machine path is committed
- [ ] No new personal data is collected or persisted; new context keys are
      low-cardinality and non-identifying

## Contract

- [ ] `packages/contracts` updated first, with examples for **both** reference
      tenants and an invalid example for any new constraint
- [ ] Change is backwards-compatible inside `v1`, or a migration plan is linked
- [ ] `pnpm run contracts:validate` passes

## Honesty

- [ ] Stubs, simulators and development-only adapters are named for what they
      are and cannot be mistaken for production behaviour
- [ ] Synthetic results are labelled synthetic and are not presented as
      commercial evidence
- [ ] No badge, comment or document claims a status that was not verified
- [ ] `docs/handoffs/current.md` reflects the state after this change
