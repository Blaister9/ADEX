# MULTI_AGENT_ORCHESTRATION.md

## Operating rule

Do not let multiple implementation agents modify the same branch or working tree concurrently. Use one branch or Git worktree per task. The repository is the shared memory; chat transcripts are not.

## Initial sequence

### Agent 1 — Foundation architect and bootstrap writer

Input: `AGENTS.md`, `PROJECT_BRIEF.md`, and `PROMPT_001_REPOSITORY_FOUNDATION.md`.

Output: branch `bootstrap/001-repository-foundation`, architecture documents, ADRs, contracts, buildable shells, local infrastructure, CI, and `docs/handoffs/current.md`.

### Agent 2 — Adversarial architecture reviewer

Starts only after Agent 1 finishes. Read-only except for a review document on its own branch. It must inspect the actual diff and run validation. It challenges unnecessary complexity, missing tenant/security boundaries, invalid contracts, toolchain choices, and non-reproducible claims. It does not rewrite the implementation.

Expected output: `docs/reviews/001-foundation-review.md` with severity-ranked findings, evidence, exact paths, and acceptance/rejection recommendation.

### Agent 3 — Foundation correction implementer

Consumes Agent 1 output and Agent 2 findings. It fixes accepted findings on a new branch, reruns all validation, and closes each review item with evidence.

### Agent 4 — First vertical slice

Implements one complete path: reference page or SDK call -> decision API -> persisted decision -> event ingestion -> attributed binary reward -> minimal analytics query. Uses uniform random first. No Thompson Sampling until this path is correct and observable.

### Agent 5 — Policy engine and simulator

Adds deterministic rules, A/B, Thompson Sampling, reproducible synthetic scenarios, offline evaluation, regret/conversion plots, and tests proving convergence under known synthetic environments. It must label synthetic results explicitly.

### Agent 6 — Administration dashboard

Builds tenant, placement, alternative, policy, and reward configuration plus analytics states. It consumes contracts rather than bypassing the API.

### Agent 7 — Security, reliability, and release reviewer

Performs tenant-isolation tests, abuse-case review, dependency/secret scan, failure injection, load baseline, backup/restore rehearsal, and release-readiness report.

## Handoff quality gate

Do not generate the next implementation prompt from a chat summary alone. Require the preceding agent's branch/diff, `docs/handoffs/current.md`, commands/results, and review findings. Any unverified claim becomes an explicit next-agent validation task.

## Prompt construction template

Each task-specific prompt should contain only:

- role;
- authoritative repository files to read;
- one objective;
- explicit in-scope paths or modules;
- approval boundaries;
- required artifacts;
- acceptance tests;
- final handoff format.

Do not repeat all permanent rules; point to `AGENTS.md`. Add task-specific detail only.
