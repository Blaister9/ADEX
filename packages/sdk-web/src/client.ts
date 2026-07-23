import { AdexError } from './errors';
import type {
  ContextValue,
  DecisionContext,
  DecisionResponse,
  EventRequest,
  EventResponse,
  PolicyReference,
  ProblemDetails,
} from './types';
import { prefixedId } from './ulid';

const DEFAULT_TIMEOUT_MS = 800;
const DECISIONS_PATH = '/v1/decisions';
const EVENTS_PATH = '/v1/events';
const API_KEY_HEADER = 'X-Adex-Api-Key';
const CORRELATION_HEADER = 'X-Correlation-Id';

export interface AdexClientOptions {
  /** Tenant-scoped publishable key. Public by design; origin-restricted server side. */
  readonly apiKey: string;
  /** Origin of the ADEX API, e.g. `https://api.example.test`. */
  readonly baseUrl: string;
  /** Client-side deadline. After it, the caller gets the fallback alternative. */
  readonly timeoutMs?: number;
  /** Injectable for tests and for hosts that wrap `fetch`. */
  readonly fetchImpl?: typeof fetch;
  /** Observability hook. The SDK never logs to the console on its own. */
  readonly onError?: (error: AdexError) => void;
}

export interface RequestDecisionInput {
  readonly placement: string;
  /** Alternative keys the page is able to render, in the page's own default order. */
  readonly eligibleAlternatives: readonly string[];
  readonly subjectId?: string;
  readonly context?: DecisionContext;
  readonly correlationId?: string;
  readonly signal?: AbortSignal;
}

export interface DecisionOutcome {
  readonly alternativeKey: string;
  /** `adex` when the server decided; `fallback` when the page's default was used. */
  readonly source: 'adex' | 'fallback';
  readonly decisionId: string | null;
  readonly policy: PolicyReference | null;
  readonly decidedAt: string | null;
  readonly error: AdexError | null;
}

export interface RecordEventInput {
  readonly type: string;
  /** Supply to make a retry idempotent; generated when omitted. */
  readonly eventId?: string;
  readonly decisionId?: string | null;
  readonly subjectId?: string;
  readonly occurredAt?: string;
  readonly properties?: Readonly<Record<string, ContextValue>>;
  readonly correlationId?: string;
  readonly signal?: AbortSignal;
}

export interface RecordEventOutcome {
  readonly eventId: string;
  readonly delivered: boolean;
  readonly status: 'accepted' | 'duplicate' | 'failed';
  readonly error: AdexError | null;
}

export interface AdexClient {
  /**
   * Never rejects. On any failure the first eligible alternative is returned
   * with `source: 'fallback'`, so a slow or unavailable ADEX cannot blank out a
   * region of the tenant's page.
   */
  requestDecision(input: RequestDecisionInput): Promise<DecisionOutcome>;
  /**
   * Never rejects. Delivery is best-effort: this foundation has no offline
   * queue, so a failed send is reported and dropped. Retrying is the caller's
   * choice and is safe, because `event_id` deduplicates server side.
   */
  recordEvent(input: RecordEventInput): Promise<RecordEventOutcome>;
}

function normalizeBaseUrl(baseUrl: string): string {
  return baseUrl.endsWith('/') ? baseUrl.slice(0, -1) : baseUrl;
}

// The guards below read the payload as an untyped record on purpose: the value
// came off the wire, so every check has to be a real runtime check rather than
// an assertion the type system already believes.
function asRecord(value: unknown): Record<string, unknown> | null {
  return typeof value === 'object' && value !== null ? (value as Record<string, unknown>) : null;
}

function isProblemDetails(value: unknown): value is ProblemDetails {
  const candidate = asRecord(value);
  return (
    candidate !== null &&
    typeof candidate.type === 'string' &&
    typeof candidate.status === 'number'
  );
}

function isDecisionResponse(value: unknown): value is DecisionResponse {
  const candidate = asRecord(value);
  return (
    candidate !== null &&
    typeof candidate.decision_id === 'string' &&
    typeof candidate.alternative_key === 'string' &&
    typeof candidate.decided_at === 'string' &&
    asRecord(candidate.policy) !== null
  );
}

function isEventResponse(value: unknown): value is EventResponse {
  const candidate = asRecord(value);
  return (
    candidate !== null &&
    typeof candidate.event_id === 'string' &&
    (candidate.status === 'accepted' || candidate.status === 'duplicate')
  );
}

export function createAdexClient(options: AdexClientOptions): AdexClient {
  if (!options.apiKey) {
    throw new AdexError('invalid_argument', 'apiKey is required.');
  }
  if (!options.baseUrl) {
    throw new AdexError('invalid_argument', 'baseUrl is required.');
  }

  const baseUrl = normalizeBaseUrl(options.baseUrl);
  const timeoutMs = options.timeoutMs ?? DEFAULT_TIMEOUT_MS;
  const doFetch = options.fetchImpl ?? globalThis.fetch.bind(globalThis);

  function report(error: AdexError): AdexError {
    options.onError?.(error);
    return error;
  }

  async function send(
    path: string,
    body: unknown,
    correlationId: string | undefined,
    signal: AbortSignal | undefined,
  ): Promise<{ status: number; payload: unknown; correlationId: string | undefined }> {
    const controller = new AbortController();
    const timer = setTimeout(() => {
      controller.abort();
    }, timeoutMs);
    const onExternalAbort = (): void => {
      controller.abort();
    };
    signal?.addEventListener('abort', onExternalAbort);

    const headers: Record<string, string> = {
      'content-type': 'application/json',
      [API_KEY_HEADER]: options.apiKey,
    };
    if (correlationId !== undefined) {
      headers[CORRELATION_HEADER] = correlationId;
    }

    try {
      const response = await doFetch(baseUrl + path, {
        method: 'POST',
        headers,
        body: JSON.stringify(body),
        signal: controller.signal,
        // Anonymous by default: no cookies are sent to the ADEX origin.
        credentials: 'omit',
        keepalive: true,
      });
      let payload: unknown = null;
      try {
        payload = (await response.json()) as unknown;
      } catch {
        payload = null;
      }
      return {
        status: response.status,
        payload,
        correlationId: response.headers.get(CORRELATION_HEADER) ?? undefined,
      };
    } catch (cause) {
      const aborted = cause instanceof Error && cause.name === 'AbortError';
      throw new AdexError(
        aborted ? 'timeout' : 'network',
        aborted ? `ADEX request exceeded ${String(timeoutMs)} ms.` : 'ADEX request failed.',
      );
    } finally {
      clearTimeout(timer);
      signal?.removeEventListener('abort', onExternalAbort);
    }
  }

  function fallback(input: RequestDecisionInput, error: AdexError | null): DecisionOutcome {
    const first = input.eligibleAlternatives[0];
    if (first === undefined) {
      throw new AdexError('invalid_argument', 'eligibleAlternatives must not be empty.');
    }
    return {
      alternativeKey: first,
      source: 'fallback',
      decisionId: null,
      policy: null,
      decidedAt: null,
      error,
    };
  }

  return {
    async requestDecision(input: RequestDecisionInput): Promise<DecisionOutcome> {
      if (input.eligibleAlternatives.length === 0) {
        throw new AdexError('invalid_argument', 'eligibleAlternatives must not be empty.');
      }

      const body: Record<string, unknown> = {
        placement: input.placement,
        eligible_alternatives: input.eligibleAlternatives.map((key) => ({ key })),
      };
      if (input.subjectId !== undefined) {
        body.subject_id = input.subjectId;
      }
      if (input.context !== undefined) {
        body.context = input.context;
      }

      try {
        const result = await send(DECISIONS_PATH, body, input.correlationId, input.signal);
        if (result.status === 200 && isDecisionResponse(result.payload)) {
          return {
            alternativeKey: result.payload.alternative_key,
            source: 'adex',
            decisionId: result.payload.decision_id,
            policy: result.payload.policy,
            decidedAt: result.payload.decided_at,
            error: null,
          };
        }
        const problem = isProblemDetails(result.payload) ? result.payload : undefined;
        return fallback(
          input,
          report(
            new AdexError(
              result.status === 200 ? 'invalid_response' : 'http',
              `ADEX returned status ${String(result.status)}.`,
              {
                status: result.status,
                ...(problem ? { problem } : {}),
                ...(result.correlationId !== undefined
                  ? { correlationId: result.correlationId }
                  : {}),
              },
            ),
          ),
        );
      } catch (cause) {
        if (cause instanceof AdexError && cause.kind === 'invalid_argument') {
          throw cause;
        }
        const error =
          cause instanceof AdexError
            ? cause
            : new AdexError('network', 'ADEX request failed unexpectedly.');
        return fallback(input, report(error));
      }
    },

    async recordEvent(input: RecordEventInput): Promise<RecordEventOutcome> {
      const eventId = input.eventId ?? prefixedId('evt_');
      const body: EventRequest = {
        event_id: eventId,
        type: input.type,
        occurred_at: input.occurredAt ?? new Date().toISOString(),
        ...(input.decisionId !== undefined ? { decision_id: input.decisionId } : {}),
        ...(input.subjectId !== undefined ? { subject_id: input.subjectId } : {}),
        ...(input.properties !== undefined ? { properties: input.properties } : {}),
      };

      try {
        const result = await send(EVENTS_PATH, body, input.correlationId, input.signal);
        if (result.status === 202 && isEventResponse(result.payload)) {
          return {
            eventId: result.payload.event_id,
            delivered: true,
            status: result.payload.status,
            error: null,
          };
        }
        const problem = isProblemDetails(result.payload) ? result.payload : undefined;
        return {
          eventId,
          delivered: false,
          status: 'failed',
          error: report(
            new AdexError(
              result.status === 202 ? 'invalid_response' : 'http',
              `ADEX returned status ${String(result.status)}.`,
              {
                status: result.status,
                ...(problem ? { problem } : {}),
                ...(result.correlationId !== undefined
                  ? { correlationId: result.correlationId }
                  : {}),
              },
            ),
          ),
        };
      } catch (cause) {
        const error =
          cause instanceof AdexError
            ? cause
            : new AdexError('network', 'ADEX request failed unexpectedly.');
        return { eventId, delivered: false, status: 'failed', error: report(error) };
      }
    },
  };
}
