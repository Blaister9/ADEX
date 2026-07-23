# AGENTS.md

## Mission

Build ADEX, a reusable multi-tenant adaptive decision engine that can be installed on arbitrary websites. The product receives anonymous context and eligible alternatives, selects an alternative under a configurable policy, returns a traceable decision, receives subsequent events/rewards, and exposes analytics. Domain-specific businesses configure ADEX; the core must not contain odontological, retail, education, real-estate, or other vertical-specific logic.

## Permanent product invariants

1. The core vocabulary is: tenant, placement, context, alternative, policy, decision, event, reward, experiment, constraint, and attribution.
2. The browser SDK is a thin integration layer. It collects approved context, requests decisions, applies configured variants, and emits events. It does not contain the authoritative policy engine.
3. The authoritative backend is ASP.NET Core/C# on a stable supported release.
4. The web SDK is TypeScript and must be framework-agnostic.
5. Python is reserved for simulation, offline experimentation, evaluation, and model research. Production online decisions must not depend on a Python service in the first release.
6. PostgreSQL is the system of record. Redis may be used for caching, rate control, and low-latency transient state.
7. The initial policies are deterministic rules, uniform random/A-B, and Thompson Sampling. More complex contextual bandits are added only after the contracts, observability, attribution, and offline evaluation are correct.
8. Multi-tenancy, auditability, idempotency, privacy, and reproducibility are architectural requirements, not later additions.
9. No personally identifiable information is required by default. Anonymous identifiers must be first-party, rotatable, and configurable.
10. Synthetic data validates engineering and algorithm behavior; it must never be presented as evidence of real commercial uplift.

## Autonomy and approval boundaries

For requests to inspect, explain, review, diagnose, or plan: inspect all relevant repository material and report findings. Do not change product code unless the task explicitly requests implementation.

For requests to build, change, or fix: make all in-scope local changes, run non-destructive validation, update documentation, and create commits on the assigned branch without asking for routine confirmation.

The agent is authorized to create and push a task branch when the task explicitly requests it. Never push directly to `main`, merge a pull request, change branch protection, rotate credentials, purchase services, deploy to production, delete remote resources, rewrite shared history, or materially expand scope without explicit human authorization.

Never expose or commit secrets. Use `.env.example`, secret stores, or CI secret references.

## Repository workflow

1. Start by running and recording:
   - `git status --short --branch`
   - `git remote -v`
   - `git log -5 --oneline`
   - a concise repository tree
2. Read this file, the project brief, architecture decisions, current handoff, and task prompt before editing.
3. Work on one task branch using the convention `<type>/<issue-or-sequence>-<short-name>`.
4. Do not edit files outside the task's declared ownership unless required for compilation or integration; document every cross-boundary edit.
5. Keep commits small, coherent, and buildable.
6. Never claim a command, test, build, migration, or deployment succeeded unless it was executed and its result was inspected.
7. End every task by updating `docs/handoffs/current.md`.

## Baseline repository shape

```text
/
├─ AGENTS.md
├─ README.md
├─ PROJECT_BRIEF.md
├─ .editorconfig
├─ .gitignore
├─ .env.example
├─ docker-compose.yml
├─ apps/
│  └─ dashboard/                 # TypeScript web administration and analytics UI
├─ packages/
│  ├─ sdk-web/                   # Framework-agnostic browser SDK
│  └─ contracts/                 # OpenAPI/JSON Schema/generated clients when justified
├─ src/
│  ├─ Adex.Api/                  # HTTP API composition root
│  ├─ Adex.Domain/               # Core domain model and policy abstractions
│  ├─ Adex.Application/          # Use cases and ports
│  └─ Adex.Infrastructure/       # PostgreSQL, Redis, telemetry, auth implementations
├─ simulation/
│  └─ adex-simulator/            # Python synthetic environment and offline evaluation
├─ tests/
│  ├─ unit/
│  ├─ integration/
│  ├─ contract/
│  └─ e2e/
├─ infra/
│  ├─ local/
│  └─ deploy/
├─ docs/
│  ├─ architecture/
│  ├─ adr/
│  ├─ api/
│  ├─ security/
│  ├─ planning/
│  └─ handoffs/
└─ .github/
   ├─ workflows/
   └─ pull_request_template.md
```

This is a baseline, not permission to create empty ceremonial projects. Any change requires an ADR explaining the trade-off.

## Architecture rules

- Prefer a modular monolith for the first production-capable release. Preserve module boundaries so high-load components can be extracted later.
- Use ports and adapters only where they protect a real boundary; avoid abstraction without a current use.
- Keep policy selection pure and testable where possible.
- Every decision must include a stable `decision_id`, tenant, placement, policy/version, eligible alternatives, selected alternative, timestamp, and sufficient audit metadata to reproduce or explain the selection.
- Event ingestion must support idempotency.
- Reward attribution must be explicit, configurable, and versioned.
- Tenant isolation must be enforced in application and persistence layers and covered by automated tests.
- Public APIs are contract-first and versioned.
- Backward-incompatible API or event-schema changes require a migration plan.
- Use UTC internally.
- Add OpenTelemetry-compatible logs, metrics, and traces from the beginning.
- Optimize only from measured bottlenecks. Do not introduce C++, WebAssembly, Kafka, Kubernetes, ClickHouse, or microservices without a benchmark or ADR that demonstrates the need.

## Engineering quality

- Pin toolchain and dependency versions. Prefer stable non-preview releases and record them.
- Enable strict compiler/type-checking settings.
- Treat warnings as errors where practical.
- Apply formatting and linting in CI.
- Require unit tests for policy logic, contract tests for APIs, integration tests for persistence, and at least one end-to-end vertical path.
- Use deterministic seeds in simulations and tests.
- Include failure paths: timeouts, invalid tenant, unknown placement, no eligible alternatives, duplicate events, unavailable Redis, and database retry behavior.
- Do not create fake implementations that silently pass as production behavior. Name stubs and simulators explicitly.

## Security and privacy

- Threat-model public SDK and ingestion endpoints.
- Use tenant-scoped credentials and allowed-origin configuration.
- Validate payload size, schema, rate, timestamps, and identifiers at boundaries.
- Do not accept arbitrary executable code as tenant configuration.
- Minimize browser fingerprinting and context collection.
- Define retention and deletion behavior before storing production events.
- Add dependency and secret scanning to CI.
- Record security decisions in `docs/security/`.

## Documentation and handoff contract

Every agent must leave durable repository state. Chat output is secondary.

`docs/handoffs/current.md` must contain:

```markdown
# Current handoff

## Task
## Branch and commit
## Objective completed
## Files changed
## Architecture decisions
## Commands executed
## Tests and results
## Known failures
## Risks and unresolved questions
## Recommended next task
## Paths the next agent should read first
```

Architecture decisions use ADRs with: context, decision, alternatives, consequences, status, and date.

## Completion standard

A task is complete only when the requested behavior exists, relevant tests pass, documentation is updated, the diff has been reviewed, and the handoff identifies any remaining uncertainty. A partial implementation must be labeled partial.
