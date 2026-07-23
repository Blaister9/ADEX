# ADEX — Adaptive Decision Engine

ADEX is an installable decision and experimentation layer for websites. A client
integrates a small TypeScript SDK, declares placements and eligible
alternatives, and ADEX selects what to show, records a traceable decision,
ingests the behavioural events that follow, attributes configurable rewards, and
reports policy performance.

The engine is domain-neutral by construction. It knows about tenants,
placements, context, alternatives, policies, decisions, events, rewards,
experiments, constraints and attribution — and nothing about any particular
industry. Industry meaning lives entirely in tenant configuration, and a CI
check enforces it.

**Status: foundation.** The architecture, contracts, component shells, local
infrastructure and quality automation exist and are verified. The product does
not. Read [What is real and what is not](#what-is-real-and-what-is-not) before
drawing any conclusion from a green build.

## Start here

| If you are… | Read |
| --- | --- |
| a coding agent picking up the work | [`docs/handoffs/current.md`](docs/handoffs/current.md), then [`AGENTS.md`](AGENTS.md) |
| deciding what to build next | [`docs/planning/foundation-roadmap.md`](docs/planning/foundation-roadmap.md) |
| trying to understand the system | [`docs/architecture/system-context.md`](docs/architecture/system-context.md) |
| about to change the HTTP surface | [`packages/contracts/README.md`](packages/contracts/README.md) and [ADR-0006](docs/adr/0006-api-contract-and-versioning.md) |
| wondering why something is the way it is | [`docs/adr/README.md`](docs/adr/README.md) |

## Prerequisites

| Tool | Version | Pinned in |
| --- | --- | --- |
| .NET SDK | 10.0.301 (LTS) | `global.json` |
| Node.js | 22.x LTS | `.nvmrc`, `package.json#engines` |
| pnpm | 11.x | `package.json#packageManager` |
| Python | 3.12 | `simulation/adex-simulator/pyproject.toml` |
| Docker + Compose | any current version | — |

Rationale for every version: [ADR-0002](docs/adr/0002-toolchain-and-runtime-baseline.md).
You only need the toolchain for the surface you are working on; each section
below stands alone.

## Clone and set up

```bash
git clone https://github.com/Blaister9/ADEX.git
cd ADEX
cp .env.example .env
```

`.env` is git-ignored. The values in `.env.example` are local development
placeholders — they are not secrets and must not be reused anywhere shared.

## Local infrastructure

```bash
docker compose up -d
docker compose ps
```

PostgreSQL listens on `127.0.0.1:55432` and Redis on `127.0.0.1:56379`, both
loopback-only on non-default ports so a locally installed server keeps its own.
`docker compose down -v` resets the database; migrations are forward-only, so
that is the supported reset (see [`infra/local/README.md`](infra/local/README.md)).

## Backend (.NET)

```bash
dotnet build Adex.slnx -c Release
dotnet test Adex.slnx -c Release
dotnet run --project src/Adex.Api            # http://localhost:5080
```

Warnings are errors. Integration tests that need containers **skip** unless you
opt in — a green suite must never mean "the infrastructure was absent, so we
checked nothing":

```bash
docker compose up -d
ADEX_INTEGRATION=1 dotnet test tests/integration/Adex.IntegrationTests -c Release
```

Try it:

```bash
curl -s http://localhost:5080/health/ready
```

```bash
curl -s -X POST http://localhost:5080/v1/decisions -H "Content-Type: application/json" -H "X-Adex-Api-Key: pk_dev_reference_services" -d '{"placement":"homepage.primary-cta","eligible_alternatives":[{"key":"variant-a"},{"key":"variant-b"}]}'
```

## TypeScript (SDK, dashboard, contracts)

```bash
pnpm install
pnpm run verify        # lint + typecheck + build + test + domain-neutrality
```

Individually:

```bash
pnpm run lint
pnpm run format
pnpm run typecheck
pnpm run build
pnpm run test
pnpm run contracts:validate
pnpm run check:domain-neutrality
```

Dashboard dev server:

```bash
pnpm --filter @adex/dashboard dev      # http://localhost:5173
```

## Simulator (Python)

```bash
cd simulation/adex-simulator
python -m venv .venv
.venv/Scripts/python -m pip install -e ".[dev]"    # Windows
# .venv/bin/python -m pip install -e ".[dev]"      # macOS / Linux

python -m ruff check .
python -m ruff format --check .
python -m pytest
python -m adex_simulator --scenario services-clear-winner --rounds 20000 --seed 20260722
```

Everything the simulator prints is synthetic and labelled as such. It validates
engineering and algorithm behaviour; it is never evidence of commercial uplift.

## Repository layout

```text
apps/dashboard          Administration and analytics UI (shell)
packages/sdk-web        Framework-agnostic, dependency-free browser SDK
packages/contracts      OpenAPI + JSON Schema — the source of truth for the HTTP surface
src/Adex.*              ASP.NET Core modular monolith: Domain, Application, Infrastructure, Api
simulation/             Python offline simulation and evaluation
tests/                  unit | contract | integration | e2e, plus cross-language fixtures
infra/local             docker compose and SQL migrations
docs/                   architecture | adr | security | planning | handoffs
scripts/                repository checks
```

## What is real and what is not

Real, and covered by tests that were executed:

- the decision path in process: validation, tenant scoping, uniform-random
  selection, a full immutable audit record, and a replay check proving a stored
  decision reproduces the alternative that was served;
- idempotent event ingestion, where a replay returns `202 duplicate` and changes
  no state, and an event citing another tenant's decision is refused;
- the public contract, validated from both sides — schemas and examples in CI,
  and the running API in the contract suite;
- health endpoints that separate required from optional dependencies, so a Redis
  outage degrades rather than drains;
- a cross-language seed contract: the C# and Python implementations are held to
  the same fixture vectors.

Not real yet, and named for what it is in the code rather than dressed up:

- **persistence.** The only stores are development in-memory ones. Nothing
  survives a restart. Selecting `Adex:Persistence:Provider=Postgres` fails fast
  with a pointer to the roadmap instead of silently degrading to a cache.
- **placement configuration.** Every placement resolves to `uniform-random` v1
  because there is no configuration store.
- **tenancy.** API keys come from configuration: no hashing, no origin
  allow-list, no rotation, no rate limiting.
- **the dashboard.** A shell that probes API health and lists what does not exist.
- **policies beyond uniform random.** Deliberate: a bandit converging on a bug
  looks exactly like a bandit working ([ADR-0009](docs/adr/0009-initial-policy-sequence.md)).
- **the latency budget.** A target in the architecture docs, never measured.

The API refuses to start with any development-only adapter outside the
Development environment.

## Contributing

`AGENTS.md` is binding: contract-first changes, tenant isolation in application
*and* persistence, no secrets, no unverified claims, and a `docs/handoffs/current.md`
update at the end of every task. The pull request template mirrors it.

## Licence

No licence has been chosen yet; this is the repository owner's decision. Until
one is added, the contents are proprietary and all rights are reserved.
There is no `CODEOWNERS` file either, for the same reason: no real ownership
information exists yet, and inventing one would route reviews to nobody.
