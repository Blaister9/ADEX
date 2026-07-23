# ADR-0002 — Toolchain and runtime baseline

- Status: Superseded by ADR-0013
- Date: 2026-07-22
- Deciders: Foundation architect agent (task 001)

## Context

`AGENTS.md` requires pinned toolchains on stable, non-preview, supported
releases, and explicitly forbids selecting a technology because it is newest.
Versions were verified against the toolchains installed on the bootstrap
machine and against the public package registries (`api.nuget.org`,
`registry.npmjs.org`) at the date of this ADR.

## Decision

| Component | Pinned version | Where pinned | Why this version |
| --- | --- | --- | --- |
| .NET SDK | `10.0.301` (`rollForward: latestFeature`) | `global.json` | .NET 10 is the current **LTS**; .NET 9 is STS and already past its support window. |
| Target framework | `net10.0` | `Directory.Build.props` | Matches the LTS runtime installed (`Microsoft.NETCore.App 10.0.9`). |
| ASP.NET Core packages | `10.0.9` | `Directory.Packages.props` | Matched to the installed shared runtime to avoid an implicit runtime upgrade requirement. |
| Npgsql | `10.0.3` | `Directory.Packages.props` | Major line aligned with .NET 10. |
| StackExchange.Redis | `2.13.17` | `Directory.Packages.props` | Mature 2.x line; the 3.x line is deliberately **not** adopted yet — no feature in it is required. |
| OpenTelemetry .NET | `1.17.0` | `Directory.Packages.props` | Current stable of the 1.x line. |
| xUnit | `xunit.v3 3.2.2` | `Directory.Packages.props` | Current stable v3 line, first-class `Microsoft.Testing.Platform` support. |
| Node.js | `22` (`>=22.13.0 <23`) | `.nvmrc`, `package.json#engines` | Node 22 "Jod" is the active LTS. Node 24 was not selected: no required feature, and 22 is the widest-supported LTS for CI images. |
| pnpm | `11.6.0` | `package.json#packageManager` | Workspace support, strict node_modules, content-addressable store. |
| TypeScript | `5.9.3` | root `devDependencies` | Deliberately **not** the 7.x native-port line: the surrounding ecosystem (`typescript-eslint` 8.x) targets `<6.1.0`. |
| ESLint | `9.39.5` + `typescript-eslint 8.65.0` | root `devDependencies` | Flat config, type-checked rule sets. ESLint 10 deferred until the plugin ecosystem settles. |
| Vite / Vitest | `7.3.6` / `4.1.10` | root `devDependencies` | Coherent peer range (`vitest 4` accepts `vite ^7`). |
| Python | `3.12` (`>=3.12,<3.13`) | `simulation/adex-simulator/pyproject.toml` | Matches the installed interpreter; 3.12 is in full support. Simulation is offline-only, so its runtime is decoupled from the API. |
| PostgreSQL | `17-alpine` | `docker-compose.yml` | Mature major with long remaining support; the newest major was not chosen for a system of record. |
| Redis | `7.4-alpine` | `docker-compose.yml` | Mature line; ADEX uses only core data structures, so nothing in a newer major is required. |

## Alternatives considered

- **Latest of everything** (TypeScript 7, ESLint 10, Redis 8, PostgreSQL 18,
  StackExchange.Redis 3). Rejected: no required capability, and each one widens
  the compatibility surface of a foundation that other agents must be able to
  build without debugging tooling.
- **.NET 9 STS.** Rejected: shorter support window than the project horizon.
- **Node 24.** Rejected: no required capability over Node 22 LTS.

## Consequences

- Upgrades are explicit, reviewable edits to `global.json`,
  `Directory.Packages.props`, `.nvmrc` and `package.json`.
- CI must install exactly these versions; `.github/workflows/ci.yml` reads
  `global.json` and `.nvmrc` rather than hard-coding versions a second time.
- The container image tags (`postgres:17-alpine`, `redis:7.4-alpine`) are pinned
  by major/minor, not by digest. Digest pinning is deferred to the deployment
  task, where an image registry and a scanning policy will exist.
- The bootstrap agent could not pull the container images (Docker daemon not
  running in the bootstrap environment), so image availability is asserted from
  the published tag scheme, not from a verified pull. See
  `docs/handoffs/current.md`.
