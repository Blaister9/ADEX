/**
 * ADEX browser SDK — a thin, framework-agnostic integration layer.
 *
 * What this package is:
 *   - a typed client for `POST /v1/decisions` and `POST /v1/events`,
 *   - a client-side fallback so a slow or unavailable ADEX never blanks out a
 *     region of the host page,
 *   - a dependency-free ULID generator for idempotent event identifiers.
 *
 * What this package is deliberately NOT:
 *   - a policy engine. Selection is authoritative on the server; nothing here
 *     may influence which alternative is chosen (ADR-0003).
 *   - a storage or consent layer. This version never persists a subject
 *     identifier; the caller supplies one if it has one. The storage and
 *     consent design is task 004 in `docs/planning/foundation-roadmap.md`.
 *   - a DOM manipulation library. The host page decides how to render an
 *     alternative; the SDK never writes HTML (threat T9).
 */

export { createAdexClient } from './client';
export type {
  AdexClient,
  AdexClientOptions,
  DecisionOutcome,
  RecordEventInput,
  RecordEventOutcome,
  RequestDecisionInput,
} from './client';
export { AdexError } from './errors';
export type { AdexFailureKind } from './errors';
export type {
  ContextValue,
  DecisionContext,
  DecisionRequest,
  DecisionResponse,
  EligibleAlternative,
  EventIngestionStatus,
  EventRequest,
  EventResponse,
  PolicyReference,
  ProblemDetails,
  ProblemError,
} from './types';
export { prefixedId, ulid } from './ulid';
export type { UlidOptions } from './ulid';
