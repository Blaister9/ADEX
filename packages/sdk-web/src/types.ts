/**
 * Wire types for the ADEX public v1 contract.
 *
 * These are declared locally on purpose: the SDK is the artefact a tenant
 * installs, so it must be self-contained and free of runtime dependencies.
 * `test/contract-alignment.test.ts` asserts at compile time that these types
 * stay mutually assignable with `@adex/contracts`, so drift fails CI.
 */

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
  readonly type: string;
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
