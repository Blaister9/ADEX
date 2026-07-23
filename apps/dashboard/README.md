# @adex/dashboard

Administration and analytics UI for ADEX. It consumes the public API contract
like any other client and has no database access of its own (ADR-0005).

## What exists today

A shell that probes `GET /health/ready` and reports whether the API is healthy,
degraded (an optional dependency such as Redis is down) or unhealthy (a required
dependency such as PostgreSQL is down), plus an explicit list of the screens that
do not exist yet.

That is the whole application. Configuration and analytics screens are roadmap
task 006; shipping empty screens now would make the dashboard look more finished
than it is.

## Commands

```bash
pnpm --filter @adex/dashboard dev
pnpm --filter @adex/dashboard build
pnpm --filter @adex/dashboard typecheck
pnpm vitest run --project dashboard
```

`VITE_ADEX_API_BASE_URL` selects the API origin; it defaults to
`http://localhost:5080`, which is what `docker compose` plus `dotnet run` gives
you locally.
