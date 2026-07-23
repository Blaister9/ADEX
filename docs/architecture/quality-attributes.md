# Quality attributes

Each attribute below has a definition, a way to tell whether it holds, and the
mechanism that is supposed to make it hold. Where a mechanism does not exist yet,
that is stated rather than implied.

## 1. Correctness and explainability of each decision

*Definition.* For any `decision_id`, an operator can reconstruct why that
alternative was chosen, from stored data alone.

*Verification.* Replay test: recompute the selection from the stored inputs and
seed; it must equal what was served. Required for every policy before it ships.

*Mechanism.* Immutable decision records with full inputs, pure selection
functions in `Adex.Domain`, seeded randomness (`docs/architecture/decision-lifecycle.md`).

*Status.* Pure selection and seeding implemented; durable audit persistence is
pending (task 004).

## 2. Tenant isolation

*Definition.* No request authenticated for tenant A can observe or affect data
of tenant B.

*Verification.* A dedicated isolation suite covering every public endpoint,
required in CI. Includes cross-tenant `decision_id` references, `404`-not-`403`
semantics, and pooled-connection setting leakage.

*Mechanism.* Explicit tenant argument in every use case, composite keys,
PostgreSQL RLS backstop (ADR-0007).

*Status.* Transport and application-level scoping implemented; RLS lands with
the persistence adapters.

## 3. Low decision latency

*Definition.* p95 server time ≤ 30 ms under early-stage load, with the segment
budget in `decision-lifecycle.md`.

*Verification.* A load baseline with a published profile and result. Not yet
run — this is an explicit gap.

*Mechanism.* Single process, no cross-service hop, cache for configuration,
pure in-process selection, one write.

*Status.* Budget defined, unmeasured. Do not quote these numbers as achieved.

## 4. Safe degradation

*Definition.* Redis absence degrades latency, never correctness or availability.
Telemetry backend absence changes nothing. PostgreSQL absence fails closed and
the SDK falls back client-side.

*Verification.* Failure-injection tests: run the API suite with Redis stopped;
run the SDK suite against a dead endpoint; assert readiness reports `degraded`
rather than failing.

*Mechanism.* Redis is never on the correctness path (ADR-0004); readiness
separates required from optional dependencies (ADR-0011); SDK timeout and
fallback (`decision-lifecycle.md`).

*Status.* Health separation and SDK fallback implemented; failure-injection
suite is a roadmap item.

## 5. Reproducibility

*Definition.* Given the same inputs and seeds, policies, simulations and
evaluations produce identical results.

*Verification.* Seeded unit tests in `Adex.Domain`; a determinism test in the
Python simulator that asserts byte-identical output for a fixed seed;
cross-language fixture equivalence before any policy is promoted (ADR-0003).

*Mechanism.* Injected seeds, no ambient RNG, no clock reads inside domain logic,
pinned toolchains (ADR-0002).

*Status.* Implemented for `uniform-random` and for the simulator's environment
generator. C# and Python consume the same seed vectors and the same policy
selection/propensity vectors.

## 6. Simple integration and removal

*Definition.* A developer adds ADEX with a script tag and a few lines, and
removing it leaves the page working exactly as before.

*Verification.* The reference integration must run with the SDK disabled and
render its default content unchanged.

*Mechanism.* Zero-dependency, framework-agnostic SDK; a client-side default for
every placement; no global side effects beyond one namespaced object.

*Status.* SDK client implemented; the reference integration page is a roadmap
item (task 004).

## 7. Auditable configuration and policy versions

*Definition.* Any configuration change that could alter decisions is versioned
and attributable to a time and an actor.

*Verification.* Changing a placement's policy configuration produces a new
policy version, and decisions before and after cite different versions.

*Mechanism.* Versioned policy bindings and attribution rules; policy state never
crosses versions (ADR-0009).

*Status.* Modelled and represented in the contract; the configuration store is
pending (task 006).

## 8. Privacy by default

*Definition.* A default installation collects no personal data and sets no
third-party storage.

*Verification.* Review of every field the SDK can send against the context
allow-list; a test that the SDK operates with storage denied; contract-level
rejection of undeclared context keys.

*Mechanism.* ADR-0008 in full.

*Status.* Contract-level allow-list defined and validated; SDK storage/consent
layer is a roadmap item — the current SDK does not persist a subject id at all,
which is the conservative failure mode.

## 9. Operability

*Definition.* An operator can answer "is it up", "is it slow", "what broke", and
"why did this decision happen" without reading source code.

*Verification.* Health endpoints return structured status; traces carry the
required attributes; an operator runbook exists per known failure mode.

*Mechanism.* ADR-0011, plus the failure table in `decision-lifecycle.md`.

*Status.* Health endpoints and OpenTelemetry traces, metrics and structured
request logs are implemented. In-memory signal tests verify the required
request fields and the optional-cache degraded counter without forbidden
high-cardinality tags. Dashboards, alerts and runbooks are roadmap items.

## 10. Domain neutrality

*Definition.* The core contains no vertical vocabulary or logic; two unrelated
reference tenants run on the same binary with different configuration only.

*Verification.* A CI check greps the core for a forbidden-vocabulary list, and
contract examples cover two unrelated tenants.

*Mechanism.* Fixed vocabulary (ADR references and `domain-model.md`), reference
tenants in the contract examples.

*Status.* Implemented, including the automated vocabulary check
(`scripts/check-domain-neutrality.mjs`).

## Attribute conflicts and how they are resolved

| Tension | Resolution |
| --- | --- |
| Latency vs auditability | Auditability wins: the decision row is written before responding. Optimize the write, do not skip it. |
| Latency vs isolation | Isolation wins: RLS and composite keys stay even if they cost index size. |
| Explainability vs policy sophistication | Explainability gates sophistication: a policy ships only when its decisions can be replayed (ADR-0009). |
| Privacy vs personalization quality | Privacy wins by default; better signals must be opt-in tenant configuration with a documented basis. |
| Simplicity vs future scale | Simplicity wins until a measurement says otherwise; that measurement becomes an ADR (ADR-0001). |
