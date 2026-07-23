# Current handoff

## Task

**001 — Repository foundation and architecture baseline**
(`PROMPT_001_REPOSITORY_FOUNDATION.md`).

Turn a repository containing four instruction documents into a coherent,
buildable, verifiable foundation: architecture, ADRs, contracts, component
shells, local infrastructure, quality automation and documentation. Explicitly
**not** the product.

### Deviation from the prompt, recorded deliberately

`PROMPT_001_REPOSITORY_FOUNDATION.md` §2 names the branch
`bootstrap/001-repository-foundation`. The human operator instructed this task
to run on **`feature/001-foundation`** and to push only that branch. The
operator's instruction was followed. No `bootstrap/*` branch was created, and
nothing else in the prompt was changed. If a later process depends on the
`bootstrap/*` name, this branch can be renamed or cherry-picked; the content is
identical either way.

## Branch and commits

Branch: `feature/001-foundation` (from `cc586fa`, shared by `main` and `dev`).

| Commit | Subject |
| --- | --- |
| `fe0d994` | chore: pin toolchains and add repository-wide governance files |
| `25eba1f` | docs: add architecture baseline, ADRs, threat model and roadmap |
| `2ccb41a` | feat: add API contracts, browser SDK and dashboard shell |
| `02f2000` | feat(api): add the .NET modular monolith with unit, contract and integration tests |
| `1640d5c` | feat(simulation): add the offline simulator and the cross-language seed contract |
| `13642a3` | feat(infra): add local Docker infrastructure and the bootstrap migration |
| `422fddc` | ci: add workflows, secret scanning, domain-neutrality check and PR template — **also carries `README.md`**, which was staged with it |
| _branch tip_ | docs: add the task 001 handoff, and allowlist the development tenant keys for the secret scan |

The tip commit is the one carrying this file, so its own hash cannot be written
inside it. `git log --oneline dev..feature/001-foundation` is authoritative.

Compare against `dev`:
`https://github.com/Blaister9/ADEX/compare/dev...feature/001-foundation`

**No pull request was opened.** `AGENTS.md` and the task prompt withhold that
authorisation; the comparison link above is the intended review entry point.

## Repository state found

Four tracked files, one commit (`cc586fa`), `main`, `dev` and
`feature/001-foundation` all pointing at it, working tree clean:

- `AGENTS.md`, `PROJECT_BRIEF.md`, `PROMPT_001_REPOSITORY_FOUNDATION.md`,
  `MULTI_AGENT_ORCHESTRATION.md`

No source, no build files, no CI, no licence, no `.gitignore`. Nothing was
overwritten or deleted; the four documents are untouched.

Environment as found: .NET SDK 10.0.301 (runtime 10.0.9), Node 22.22.3, pnpm
11.6.0, Python 3.12.6, Docker 28.0.4 with Compose v2.34.0 — **daemon not
running** at the start (Docker Desktop was started during the task; see
verification below).

## Objective completed

Everything the prompt asked for is present and, except for the two items under
[Known failures and unverified checks](#known-failures-and-unverified-checks),
was executed and inspected:

- toolchains pinned and justified; architecture set and 12 ADRs; threat model;
  roadmap;
- contract-first OpenAPI 3.1 + JSON Schema with two unrelated reference tenants
  and rejection examples, validated in CI;
- a .NET modular monolith that builds warning-free and serves the two v1
  endpoints, with 115 passing tests across unit, contract and integration;
- a dependency-free browser SDK and a dashboard shell;
- an offline Python simulator sharing a fixture-enforced seed contract with C#;
- Docker Compose infrastructure that was actually started, with the bootstrap
  migration applied and verified;
- CI mirroring every documented local command, plus secret scanning and a
  mechanical domain-neutrality check.

## Files changed

197 files, +14,080 lines, no deletions. Highlights rather than an inventory
(`git diff --stat dev...HEAD` is authoritative):

| Area | Key paths |
| --- | --- |
| Governance | `.gitignore`, `.gitattributes`, `.editorconfig`, `.env.example`, `global.json`, `NuGet.config`, `Directory.Build.props`, `Directory.Packages.props`, `package.json`, `pnpm-workspace.yaml`, `eslint.config.js`, `.prettierrc.json`, `vitest.config.ts`, `.gitleaks.toml` |
| Documentation | `README.md`, `docs/architecture/*` (6), `docs/adr/*` (12 + index), `docs/security/threat-model-initial.md`, `docs/planning/foundation-roadmap.md` |
| Contracts | `packages/contracts/openapi/adex-public-v1.yaml`, `schemas/*.schema.json` (6), `examples/*` (14 incl. 6 that must fail), `src/index.ts`, `test/*` |
| Backend | `Adex.slnx`, `src/Adex.Domain/*`, `src/Adex.Application/*`, `src/Adex.Infrastructure/*`, `src/Adex.Api/*` |
| SDK / UI | `packages/sdk-web/*`, `apps/dashboard/*` |
| Simulation | `simulation/adex-simulator/*` |
| Tests | `tests/unit/*`, `tests/contract/*`, `tests/integration/*`, `tests/fixtures/decision-seed-vectors.json`, `tests/e2e/README.md` |
| Infrastructure | `docker-compose.yml`, `infra/local/migrations/0001_bootstrap.sql`, `infra/local/README.md`, `infra/deploy/README.md` |
| Automation | `.github/workflows/ci.yml`, `.github/pull_request_template.md`, `scripts/check-domain-neutrality.mjs` |

## Architecture decisions

Twelve ADRs in `docs/adr/`, all Accepted, dated 2026-07-22:

| ADR | Decision |
| --- | --- |
| 0001 | Modular monolith, with four named extraction seams, over microservices |
| 0002 | Toolchain baseline: .NET 10 LTS, Node 22 LTS, pnpm 11, TypeScript 5.9, Python 3.12, PostgreSQL 17, Redis 7.4 — maturity over recency, each pin justified |
| 0003 | Language boundaries: C# owns the online path, TypeScript integrates, Python is offline-only; policies crossing the boundary are validated by shared fixtures |
| 0004 | PostgreSQL is the only system of record; Redis is optional, degradable, never on the correctness path |
| 0005 | Monorepo with native per-ecosystem tooling, no meta-build system |
| 0006 | Contract-first, URL-path major versioning, snake_case, UTC-only, RFC 9457 errors; explicit compatibility table |
| 0007 | Shared schema with mandatory `tenant_id`, composite keys and a PostgreSQL RLS backstop; `404` not `403` for foreign objects |
| 0008 | Anonymous first-party rotatable subject ids, allow-listed low-cardinality context, salted HMAC for tenant-supplied ids, no fingerprinting |
| 0009 | Policy sequence rules → A/B → Thompson Sampling → contextual, each stage gated by evidence; propensity logged from stage 2 |
| 0010 | SQL-first forward-only migrations over an ORM's migration generator |
| 0011 | OpenTelemetry for all three signals, OTLP-optional, with a metric-cardinality rule and audit-vs-telemetry separation |
| 0012 | Client-generated `event_id`, database-enforced uniqueness, `202 duplicate` on replay, reward written in the same transaction |

Three decisions made during implementation and worth flagging to a reviewer:

1. **PostgreSQL is only a *required* readiness dependency when it actually backs
   the stores.** While the provider is `InMemory` the API does not depend on it,
   and claiming otherwise would make readiness lie.
2. **ASP.NET's `traceId` is stripped from problem responses** in favour of the
   existing `correlation_id`: two identifiers under two naming conventions is
   worse than one.
3. **xUnit1051 is suppressed for test projects** (documented in
   `Directory.Build.props`) — these suites finish in milliseconds, so threading a
   cancellation token through every call buys nothing.

## Commands executed and results

Every line below was run in this environment and its output inspected.

| # | Command | Result |
| --- | --- | --- |
| 1 | `dotnet build Adex.slnx -c Release` | **Pass** — 0 warnings, 0 errors (warnings-as-errors is on) |
| 2 | `dotnet test tests/unit/Adex.UnitTests -c Release` | **Pass** — 71/71 |
| 3 | `dotnet test tests/contract/Adex.ContractTests -c Release` | **Pass** — 34/34 |
| 4 | `dotnet test tests/integration/Adex.IntegrationTests -c Release` | **Pass** — 5 passed, 5 skipped (containers not opted in) |
| 5 | `ADEX_INTEGRATION=1 dotnet test tests/integration/...` | **Pass** — 10/10 against live containers |
| 6 | `pnpm install` | **Pass** — 4 workspace projects, lockfile written |
| 7 | `pnpm run lint` | **Pass** — clean (33 findings were fixed, not suppressed) |
| 8 | `pnpm run format` | **Pass** — all files match Prettier |
| 9 | `pnpm run typecheck` | **Pass** — 4 projects, strict + `noUncheckedIndexedAccess` + `exactOptionalPropertyTypes` |
| 10 | `pnpm run build` | **Pass** — dashboard 195.46 kB, SDK declarations emitted |
| 11 | `pnpm run test` | **Pass** — 66/66 across sdk-web, contracts, dashboard |
| 12 | `pnpm run check:domain-neutrality` | **Pass** — no vertical vocabulary in src/packages/apps/simulation/infra |
| 13 | `python -m ruff check .` | **Pass** — all checks passed |
| 14 | `python -m ruff format --check .` | **Pass** — 10 files formatted |
| 15 | `python -m pytest` | **Pass** — 28/28 |
| 16 | `python -m adex_simulator --scenario services-clear-winner --rounds 5000` | **Pass** — synthetic report with banner; regret 135.01, share-of-best 0.314 |
| 17 | `docker compose config --quiet` | **Pass** — interpolation resolves, exit 0 |
| 18 | `docker compose up -d` | **Pass** — both containers `healthy` |
| 19 | `psql \dn`, `select * from adex.schema_migrations`, `show timezone` | **Pass** — schema `adex` exists, `0001_bootstrap` recorded, timezone `UTC` |
| 20 | `redis-cli ping` | **Pass** — `PONG` |
| 21 | `dotnet run --project src/Adex.Api` + manual `curl` sequence | **Pass** — see below |
| 22 | `docker run zricethezav/gitleaks detect --config=.gitleaks.toml` | **Pass** — no leaks. The first run over the finished branch reported one: the `pk_dev_reference_services` key in a README curl example. It is a Development-only key the API refuses to accept elsewhere, so it is allowlisted by value pattern (`pk_(dev\|test)_*`) rather than by file, leaving a real key in the same file still reportable. |
| 23 | `python -c "yaml.safe_load(...ci.yml)"` | **Pass** — 6 jobs parse |

### Manual end-to-end run against a live API (command 21)

```text
POST /v1/decisions  (tenant A)  -> 200 {"decision_id":"dec_01KY6DX1TM3D2C4FREMN4R1YW5",
                                        "alternative_key":"request-callback",
                                        "policy":{"key":"uniform-random","version":1},
                                        "decided_at":"2026-07-23T02:47:01.717Z"}
POST /v1/events     (same decision)  -> 202 {"status":"accepted"}
POST /v1/events     (identical replay) -> 202 {"status":"duplicate"}
POST /v1/events     (tenant B citing tenant A's decision) -> 422 unknown_decision
POST /v1/decisions  (no API key) -> 401
GET  /health/ready  -> 200 {"status":"healthy", postgres + redis both reported}
```

### Two real defects found by running things, not by reading them

1. **Malformed JSON returned `500`.** `UseExceptionHandler` did not map
   `BadHttpRequestException` to its status. Fixed with an explicit
   `IExceptionHandler` that answers `400` and never leaks internals. Caught by a
   contract test.
2. **A healthy Redis reported as unreachable.** Compose publishes on the IPv4
   loopback while `localhost` resolves to `::1` first on this machine. All local
   defaults now use `127.0.0.1`, with the reason recorded. Caught by running the
   integration suite against real containers.

## Known failures and unverified checks

Nothing is failing. Two checks could not be executed here, stated exactly:

1. **The GitHub Actions workflow has never run.**
   - Command that would verify it: pushing to a branch covered by
     `.github/workflows/ci.yml` and inspecting the run.
   - What was done instead: the YAML was parsed, and every command inside it was
     executed locally. What remains unproven is action versions
     (`actions/setup-dotnet@v4`, `pnpm/action-setup@v4`, …), service-container
     wiring, and the `zricethezav/gitleaks` and `pip-audit` steps under Actions.
   - Next action: agent 002 pushes and reads the first run, and fixes whatever
     the runner disagrees with.

2. **Container images were pulled but never scanned or digest-pinned.**
   `postgres:17-alpine` and `redis:7.4-alpine` are pinned by major/minor, not by
   digest (ADR-0002 says why). Digest pinning belongs with the deployment task.

Deliberately not attempted, per the task's boundaries: no pull request, no
merge, no push to `main`/`dev`, no deployment, no cloud resources.

## Risks and unresolved questions

| Risk | Why it matters | Who decides |
| --- | --- | --- |
| **No durable persistence** | The largest gap by far. Every decision and event lives in process memory and dies on restart. Nothing built on top of this is trustworthy until task 004 lands. | Next agent |
| **No licence, no `CODEOWNERS`** | Both need real human information. Absent a licence, the contents are proprietary by default; absent owners, a `CODEOWNERS` file would route reviews to nobody. | Repository owner |
| **Latency budget is unmeasured** | `docs/architecture/decision-lifecycle.md` states a 30 ms p95 server target. It is a target. Nobody should quote it as achieved. | Task 007 |
| **Publishable API keys are forgeable off-browser** | Documented as accepted residual risk (threat T1). Mitigation today is rate limits and data-quality metrics — neither implemented. Signed decision tokens are the planned hardening. | Task 004 / 007 |
| **Policy logic will exist twice** (C# + Python) | Mitigated by the shared fixture vectors, but the mitigation only covers seed derivation so far. Every future policy needs its own fixtures before promotion. | Task 005 |
| **`.slnx` rather than `.sln`** | .NET 10's default solution format. Modern tooling reads it; older tooling may not. Trivially convertible if it bites. | Reviewer |
| **Redis ships in compose but nothing uses it** | Only the health probe touches it today. That is intentional (ADR-0004 makes it optional), but a reviewer should confirm it has not become decorative. | Agent 002 |

## Recommended next task

**Agent 002 — adversarial architecture review**, exactly as
`MULTI_AGENT_ORCHESTRATION.md` defines it: read-only except for
`docs/reviews/001-foundation-review.md` on its own branch.

Objective, stated precisely so it can be handed over verbatim:

> Review the diff `dev...feature/001-foundation`. Re-run every command in the
> table above and record the actual output. Challenge: unnecessary complexity,
> missing tenant or security boundaries, contract errors, toolchain choices, and
> any claim in the documentation that the repository does not support. Verify
> in particular that (a) the development-only adapters cannot be mistaken for
> production behaviour, (b) tenant isolation is genuinely enforced rather than
> merely tested, (c) the OpenAPI document and the running API actually agree,
> and (d) nothing in the core carries vertical vocabulary. Push the branch to
> trigger the GitHub Actions workflow, which has never executed, and record what
> the runner reports. Produce `docs/reviews/001-foundation-review.md` with
> severity-ranked findings, evidence, exact paths, and an accept/reject
> recommendation. Do not rewrite the implementation.

After that review closes, **task 004 (first vertical slice)** is the highest-value
work: it removes the persistence gap that every other item depends on.

## Paths the next agent should read first

1. `AGENTS.md` — binding rules, unchanged by this task
2. `PROJECT_BRIEF.md` — what the product is
3. `docs/handoffs/current.md` — this file
4. `docs/planning/foundation-roadmap.md` — what is next and what is owed
5. `docs/adr/README.md` — the twelve decisions and why
6. `docs/architecture/system-context.md` → `container-view.md` →
   `decision-lifecycle.md` → `event-and-reward-lifecycle.md`
7. `packages/contracts/README.md` and `openapi/adex-public-v1.yaml`
8. `src/README.md` — what is real in the backend and what is not
9. `docs/security/threat-model-initial.md` — including the accepted residual risks
10. `README.md` — exact commands for every surface

The repository is the source of truth. Nothing essential to continuing this work
exists only in a chat transcript.
