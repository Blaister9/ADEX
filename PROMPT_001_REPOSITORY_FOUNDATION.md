# PROMPT 001 — Repository foundation and architecture baseline

You are the lead software architect and repository bootstrap agent for ADEX. You have authorized access to the complete local repository and its configured Git remote.

Read `AGENTS.md` and `PROJECT_BRIEF.md` completely before acting. Treat them as authoritative. This task authorizes local edits, creation of a feature branch, commits, and pushing that feature branch. It does not authorize direct changes to `main`, merging, production deployment, cloud purchases, credential changes, branch-protection changes, or destructive remote operations.

## Objective

Transform the current repository into a coherent, buildable foundation for ADEX. Establish durable architecture, contracts, repository governance, local infrastructure, and component skeletons. Do not implement the complete product and do not create large volumes of placeholder code.

## Required workflow

1. Inspect before editing:
   - Run `git status --short --branch`, `git remote -v`, and `git log -5 --oneline`.
   - Inspect the complete repository tree, existing build files, licenses, workflows, and documentation.
   - Determine whether the repository is empty, partially initialized, or contains conflicting work.
   - Preserve compatible existing work. Do not overwrite unexplained files.
   - Record material findings in `docs/handoffs/current.md`.

2. Create or switch to branch:
   - `bootstrap/001-repository-foundation`
   - If that branch exists, inspect it and continue safely rather than recreating or force-resetting it.

3. Resolve and pin toolchains:
   - Use stable, non-preview, supported releases for .NET/ASP.NET Core, Node.js, the TypeScript package manager, Python, PostgreSQL, and Redis.
   - Verify versions from authoritative sources available to you.
   - Pin versions using appropriate repository files.
   - Record the choices and support rationale in an ADR. Do not select technology merely because it is newest.

4. Produce architecture documentation:
   - `docs/architecture/system-context.md`
   - `docs/architecture/container-view.md`
   - `docs/architecture/domain-model.md`
   - `docs/architecture/decision-lifecycle.md`
   - `docs/architecture/event-and-reward-lifecycle.md`
   - `docs/architecture/quality-attributes.md`
   - `docs/security/threat-model-initial.md`
   - `docs/planning/foundation-roadmap.md`
   Use Mermaid diagrams where they improve clarity, but ensure the text stands alone.

5. Produce ADRs at minimum for:
   - modular monolith versus microservices;
   - C# online decision path, TypeScript SDK, and Python simulation boundary;
   - PostgreSQL as system of record and Redis as optional acceleration;
   - monorepo organization;
   - API contract/versioning strategy;
   - tenant isolation strategy;
   - anonymous identity and privacy defaults;
   - initial policy sequence: rules, A/B, Thompson Sampling, then contextual methods after evidence.

6. Define contracts without overimplementing:
   - Add an OpenAPI contract for a minimal versioned decision endpoint.
   - Add an OpenAPI contract for minimal versioned event ingestion.
   - Add JSON Schemas for decision context, alternatives, events, and error envelopes when OpenAPI alone is insufficient.
   - Include examples for two unrelated tenants to prove domain independence.
   - Include idempotency, correlation, tenant, placement, policy version, timestamps, and error semantics.
   - Do not expose internal database entities as public API contracts.

7. Scaffold only buildable component shells:
   - ASP.NET Core solution/projects matching the approved boundaries.
   - Framework-agnostic TypeScript SDK package.
   - TypeScript dashboard shell.
   - Python simulator package.
   - Test projects/directories.
   Each component must have a health/build/smoke test or equivalent. Avoid generated demo clutter and unused dependencies.

8. Establish local infrastructure:
   - `docker-compose.yml` for PostgreSQL and Redis with health checks, named volumes, safe development defaults, and no embedded secrets.
   - `.env.example`.
   - documented startup and shutdown commands.
   - database migration mechanism selected and documented, but only a minimal bootstrap migration if needed.

9. Establish repository quality controls:
   - root README with exact clone/setup/build/test/run commands;
   - `.editorconfig`, formatting, linting, strict type/compiler settings;
   - CI for all component builds and tests;
   - dependency and secret scanning where supported;
   - pull request template;
   - CODEOWNERS only if real ownership information is known; otherwise document why it was not created.
   - no badges claiming unverified status.

10. Validate:
   - Run every documented local build, lint, test, schema validation, and Docker configuration check that is possible in the environment.
   - Inspect outputs.
   - Fix failures within scope.
   - If an environmental blocker prevents a check, record the exact command, error, likely cause, and next action. Do not describe it as passing.

11. Review the final diff:
   - Remove empty ceremonial abstractions, unused packages, generated clutter, secrets, absolute machine paths, and misleading claims.
   - Confirm domain independence by checking that the core contains no dental-specific vocabulary.
   - Confirm the repository can be understood without chat history.

12. Commit and hand off:
   - Update `docs/handoffs/current.md` using the required handoff format in `AGENTS.md`.
   - Create coherent commits.
   - Push only `bootstrap/001-repository-foundation`.
   - Do not open or merge a pull request unless the environment explicitly supports opening one and no additional authorization is required; otherwise provide the exact branch comparison target.

## Minimum public contract behavior to specify

Decision request:

```json
{
  "placement": "homepage.primary-cta",
  "subject_id": "anon_...",
  "context": {
    "device_class": "mobile",
    "referrer_group": "organic"
  },
  "eligible_alternatives": [
    {"key": "variant-a"},
    {"key": "variant-b"}
  ]
}
```

Decision response:

```json
{
  "decision_id": "dec_...",
  "alternative_key": "variant-b",
  "policy": {
    "key": "uniform-random",
    "version": 1
  },
  "decided_at": "RFC3339 UTC timestamp"
}
```

Event request:

```json
{
  "event_id": "evt_...",
  "decision_id": "dec_...",
  "type": "click",
  "occurred_at": "RFC3339 UTC timestamp",
  "properties": {}
}
```

These examples constrain semantics, not final naming. Improve them only when the improvement is documented and remains simple.

## Acceptance criteria

- The repository is not dependent on unstated chat context.
- All component shells build or have an explicitly documented environmental blocker.
- Contracts are syntactically validated.
- Docker Compose configuration validates.
- CI reflects the local validation commands.
- Architecture and ADRs are internally consistent.
- Tenant boundaries, idempotency, privacy, observability, and reproducibility appear in architecture and tests/plans.
- No production deployment occurs.
- No secret is committed.
- The final report states exact commands and results, branch, commits, changed files, unresolved risks, and recommended next task.

## Final response format

Return only:

1. Repository state found.
2. Branch and commit identifiers.
3. Architecture decisions made.
4. Files created or changed.
5. Commands executed and results.
6. Known blockers or failed checks.
7. Risks requiring human decision.
8. Exact recommended prompt objective for the next review agent.

Do not paste entire files into the response; the repository is the source of truth.
