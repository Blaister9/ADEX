/**
 * TypeScript projection of the ADEX public v1 contract.
 *
 * The authority is `openapi/adex-public-v1.yaml` and the JSON Schemas in
 * `schemas/`. These types are a hand-maintained convenience for TypeScript
 * consumers; `test/contract.test.ts` asserts that the constants below stay
 * consistent with the OpenAPI document, so drift fails CI rather than shipping.
 */

export const CONTRACT_VERSION = '1.0.0' as const;
export const API_MAJOR_VERSION = 'v1' as const;

export const V1_PATHS = {
  decisions: '/v1/decisions',
  events: '/v1/events',
  healthLive: '/health/live',
  healthReady: '/health/ready',
} as const;

export const HEADERS = {
  apiKey: 'X-Adex-Api-Key',
  correlationId: 'X-Correlation-Id',
  idempotencyKey: 'Idempotency-Key',
} as const;

export const PROBLEM_TYPE_BASE = 'https://contracts.adex.dev/problems' as const;

export const PROBLEM_TYPES = {
  validationFailed: `${PROBLEM_TYPE_BASE}/validation-failed`,
  noEligibleAlternatives: `${PROBLEM_TYPE_BASE}/no-eligible-alternatives`,
  unknownPlacement: `${PROBLEM_TYPE_BASE}/unknown-placement`,
  originNotAllowed: `${PROBLEM_TYPE_BASE}/origin-not-allowed`,
  idempotencyKeyReused: `${PROBLEM_TYPE_BASE}/idempotency-key-reused`,
  rateLimited: `${PROBLEM_TYPE_BASE}/rate-limited`,
  dependencyUnavailable: `${PROBLEM_TYPE_BASE}/dependency-unavailable`,
} as const;

/** Reserved event types with fixed meaning across every tenant. */
export const RESERVED_EVENT_TYPES = ['impression', 'click', 'lead', 'purchase'] as const;
export type ReservedEventType = (typeof RESERVED_EVENT_TYPES)[number];

/** Context keys the foundation declares. Tenants extend this list by configuration. */
export const BASE_CONTEXT_KEYS = [
  'device_class',
  'referrer_group',
  'locale',
  'page_group',
  'session_ordinal',
] as const;

export type ContextValue = string | number | boolean | null;
export type DecisionContext = Readonly<Record<string, ContextValue>>;

export interface EligibleAlternative {
  readonly key: string;
}

export interface DecisionRequest {
  readonly placement: string;
  readonly subject_id?: string;
  readonly context?: DecisionContext;
  readonly eligible_alternatives: readonly EligibleAlternative[];
}

export interface PolicyReference {
  readonly key: string;
  readonly version: number;
}

export interface DecisionResponse {
  readonly decision_id: string;
  readonly alternative_key: string;
  readonly policy: PolicyReference;
  readonly decided_at: string;
}

export interface EventRequest {
  readonly event_id: string;
  readonly decision_id?: string | null;
  readonly subject_id?: string;
  readonly type: ReservedEventType | (string & {});
  readonly occurred_at: string;
  readonly properties?: Readonly<Record<string, ContextValue>>;
}

export type EventIngestionStatus = 'accepted' | 'duplicate';

export interface EventResponse {
  readonly event_id: string;
  readonly status: EventIngestionStatus;
  readonly received_at: string;
}

export interface ProblemError {
  readonly pointer: string;
  readonly code: string;
  readonly detail?: string;
}

export interface ProblemDetails {
  readonly type: string;
  readonly title: string;
  readonly status: number;
  readonly detail?: string;
  readonly instance?: string;
  readonly correlation_id?: string;
  readonly errors?: readonly ProblemError[];
}

export type HealthStatus = 'healthy' | 'degraded' | 'unhealthy';

export interface HealthCheckResult {
  readonly name: string;
  readonly status: HealthStatus;
  /** When false, an unhealthy result degrades readiness instead of failing it. */
  readonly required: boolean;
  readonly detail?: string;
}

export interface HealthReport {
  readonly status: HealthStatus;
  readonly checks?: readonly HealthCheckResult[];
}

/** Identifier prefixes. Clients must treat the values as opaque. */
export const ID_PREFIXES = {
  tenant: 'ten_',
  decision: 'dec_',
  event: 'evt_',
  subject: 'anon_',
} as const;

/** Crockford base32 ULID body, as enforced by `schemas/common.schema.json`. */
export const ULID_PATTERN = /^[0-7][0-9A-HJKMNP-TV-Z]{25}$/;

export function isUlid(value: string): boolean {
  return ULID_PATTERN.test(value);
}

export function isPrefixedId(value: string, prefix: string): boolean {
  return value.startsWith(prefix) && isUlid(value.slice(prefix.length));
}
