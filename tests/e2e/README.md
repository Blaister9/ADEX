# End-to-end tests

**Intentionally empty.** This directory is filled by roadmap task 004.

An end-to-end suite here would exercise: reference page → `POST /v1/decisions` →
persisted decision → `POST /v1/events` → attributed Bernoulli reward → one
analytics query. That path needs durable persistence, and the foundation
deliberately ships only a development in-memory store
(`src/Adex.Infrastructure/Persistence/InMemory`). An e2e suite written against
it would prove that a cache behaves like a cache.

What task 004 must add here:

1. A reference integration page using `packages/sdk-web`, which must also render
   correct default content with the SDK disabled — removal has to be as simple
   as installation (quality attribute 6).
2. A run against `docker compose up -d` with
   `Adex:Persistence:Provider=Postgres`.
3. A replay assertion: the stored decision, recomputed offline, yields the
   alternative that was actually served.
4. A tenant-isolation pass over the whole path, not just per endpoint.

See `docs/planning/foundation-roadmap.md`.
