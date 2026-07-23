# ADR-0005 — Monorepo organization

- Status: Accepted
- Date: 2026-07-22
- Deciders: Foundation architect agent (task 001)

## Context

The product spans a browser SDK, a dashboard, a .NET service, a Python
simulator, shared API contracts, and infrastructure definitions. These evolve
together: a contract change touches the SDK, the API, the dashboard, the
contract tests, and the documentation in the same logical change.

## Decision

A single repository with the layout fixed in `AGENTS.md`:

```text
apps/dashboard          TypeScript administration and analytics UI
packages/sdk-web        Framework-agnostic browser SDK
packages/contracts      OpenAPI + JSON Schema, the source of truth for the HTTP surface
src/Adex.*              ASP.NET Core modular monolith (Domain/Application/Infrastructure/Api)
simulation/             Python offline simulation and evaluation
tests/                  unit | integration | contract | e2e
infra/local, infra/deploy
docs/                   architecture | adr | api | security | planning | handoffs
.github/workflows       CI mirroring the documented local commands
```

Rules:

1. `packages/contracts` is the **only** source of truth for the public HTTP
   surface. The API and the SDK are both validated against it; neither generates
   it implicitly at build time in this phase.
2. Each ecosystem keeps its native workspace mechanism: a pnpm workspace for
   TypeScript, one `.sln` with central package management for .NET, a
   `pyproject.toml` for Python. No cross-ecosystem meta-build tool is
   introduced.
3. Test code lives under `tests/` by test kind, not next to the production code,
   so the CI matrix maps one-to-one to directories.
4. Every top-level directory has a `README.md` explaining ownership and how to
   run it.

## Alternatives considered

- **Polyrepo (one repository per component).** Rejected: contract changes would
  need synchronized releases across four repositories before the product has a
  single user.
- **Nx/Turborepo/Bazel over everything.** Rejected: adds a build system to learn
  and maintain to gain caching that a foundation-sized repository does not need.
  Revisit when CI wall time becomes a measured problem.
- **Tests co-located with sources (`src/**/*.Tests`).** Rejected: `AGENTS.md`
  fixes the `tests/<kind>` layout, and the kind-based split is what lets CI run
  unit tests without infrastructure and integration tests only when containers
  are up.

## Consequences

- A contributor must be able to work on one surface without installing all
  three toolchains; the README documents per-surface commands.
- CI runs four independent jobs (.NET, TypeScript, Python, infra/contracts) and
  a fifth that only runs when infrastructure containers are available.
- Path-based ownership is expressible later via `CODEOWNERS` once real owners
  exist.
