import { describe, expect, it, vi } from 'vitest';
import { createAdexClient } from '../src/client';
import { AdexError } from '../src/errors';

const API_KEY = 'pk_test_reference_services';
const BASE_URL = 'https://api.adex.test';

const DECISION_BODY = {
  decision_id: 'dec_01JQZ8K3N4P5R6S7T8V9W0X1Y2',
  alternative_key: 'book-appointment',
  policy: { key: 'uniform-random', version: 1 },
  decided_at: '2026-07-22T14:03:11.482Z',
};

const ACCEPTED_EVENT_BODY = {
  event_id: 'evt_01JQZ8M0Q1R2S3T4V5W6X7Y8Z9',
  status: 'accepted',
  received_at: '2026-07-22T14:04:02.640Z',
};

function jsonResponse(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json' },
  });
}

/** A stub `fetch` that always answers with the same response. */
function respondWith(status: number, body: unknown): typeof fetch {
  return () => Promise.resolve(jsonResponse(status, body));
}

function clientWith(fetchImpl: typeof fetch, onError?: (error: AdexError) => void) {
  return createAdexClient({
    apiKey: API_KEY,
    baseUrl: BASE_URL,
    fetchImpl,
    timeoutMs: 50,
    ...(onError ? { onError } : {}),
  });
}

function callArgs(spy: { mock: { calls: unknown[][] } }, index = 0): [string, RequestInit] {
  return spy.mock.calls[index] as [string, RequestInit];
}

describe('createAdexClient', () => {
  it('rejects construction without credentials or a base url', () => {
    expect(() => createAdexClient({ apiKey: '', baseUrl: BASE_URL })).toThrow(AdexError);
    expect(() => createAdexClient({ apiKey: API_KEY, baseUrl: '' })).toThrow(AdexError);
  });
});

describe('requestDecision', () => {
  it('sends the contract-shaped body with the tenant api key header', async () => {
    const fetchImpl = vi.fn(() => Promise.resolve(jsonResponse(200, DECISION_BODY)));
    const client = clientWith(fetchImpl);

    await client.requestDecision({
      placement: 'homepage.primary-cta',
      eligibleAlternatives: ['request-callback', 'book-appointment'],
      subjectId: 'anon_01JQZ7H9K2M3N4P5Q6R7S8T9V0',
      context: { device_class: 'mobile' },
    });

    expect(fetchImpl).toHaveBeenCalledTimes(1);
    const [url, init] = callArgs(fetchImpl);
    expect(url).toBe(`${BASE_URL}/v1/decisions`);
    expect((init.headers as Record<string, string>)['X-Adex-Api-Key']).toBe(API_KEY);
    expect(JSON.parse(init.body as string)).toEqual({
      placement: 'homepage.primary-cta',
      subject_id: 'anon_01JQZ7H9K2M3N4P5Q6R7S8T9V0',
      context: { device_class: 'mobile' },
      eligible_alternatives: [{ key: 'request-callback' }, { key: 'book-appointment' }],
    });
  });

  it('never sends cookies to the ADEX origin', async () => {
    const fetchImpl = vi.fn(() => Promise.resolve(jsonResponse(200, DECISION_BODY)));
    const client = clientWith(fetchImpl);
    await client.requestDecision({ placement: 'p', eligibleAlternatives: ['a'] });
    expect(callArgs(fetchImpl)[1].credentials).toBe('omit');
  });

  it('omits optional fields it was not given', async () => {
    const fetchImpl = vi.fn(() => Promise.resolve(jsonResponse(200, DECISION_BODY)));
    const client = clientWith(fetchImpl);
    await client.requestDecision({ placement: 'p', eligibleAlternatives: ['a'] });
    expect(JSON.parse(callArgs(fetchImpl)[1].body as string)).toEqual({
      placement: 'p',
      eligible_alternatives: [{ key: 'a' }],
    });
  });

  it('returns the server decision when the call succeeds', async () => {
    const client = clientWith(respondWith(200, DECISION_BODY));
    const outcome = await client.requestDecision({
      placement: 'homepage.primary-cta',
      eligibleAlternatives: ['request-callback', 'book-appointment'],
    });
    expect(outcome).toEqual({
      alternativeKey: 'book-appointment',
      source: 'adex',
      decisionId: DECISION_BODY.decision_id,
      policy: DECISION_BODY.policy,
      decidedAt: DECISION_BODY.decided_at,
      error: null,
    });
  });

  it('falls back to the first eligible alternative when ADEX fails', async () => {
    const errors: AdexError[] = [];
    const client = clientWith(
      respondWith(503, {
        type: 'https://contracts.adex.dev/problems/dependency-unavailable',
        title: 'A required dependency is unavailable.',
        status: 503,
      }),
      (error) => errors.push(error),
    );
    const outcome = await client.requestDecision({
      placement: 'homepage.primary-cta',
      eligibleAlternatives: ['request-callback', 'book-appointment'],
    });
    expect(outcome.source).toBe('fallback');
    expect(outcome.alternativeKey).toBe('request-callback');
    expect(outcome.decisionId).toBeNull();
    expect(errors).toHaveLength(1);
    expect(errors[0]?.kind).toBe('http');
    expect(errors[0]?.status).toBe(503);
    expect(errors[0]?.problem?.type).toContain('dependency-unavailable');
  });

  it('falls back when the network throws, and never rejects', async () => {
    const client = clientWith(() => {
      throw new TypeError('connection refused');
    });
    const outcome = await client.requestDecision({
      placement: 'p',
      eligibleAlternatives: ['a', 'b'],
    });
    expect(outcome.source).toBe('fallback');
    expect(outcome.alternativeKey).toBe('a');
    expect(outcome.error?.kind).toBe('network');
  });

  it('falls back when the deadline passes', async () => {
    const never = ((_url: string, init?: RequestInit) =>
      new Promise<Response>((_resolve, reject) => {
        init?.signal?.addEventListener('abort', () => {
          const abortError = new Error('aborted');
          abortError.name = 'AbortError';
          reject(abortError);
        });
      })) as unknown as typeof fetch;

    const outcome = await clientWith(never).requestDecision({
      placement: 'p',
      eligibleAlternatives: ['a', 'b'],
    });
    expect(outcome.source).toBe('fallback');
    expect(outcome.error?.kind).toBe('timeout');
  });

  it('falls back when the body does not match the contract', async () => {
    const client = clientWith(respondWith(200, { unexpected: true }));
    const outcome = await client.requestDecision({ placement: 'p', eligibleAlternatives: ['a'] });
    expect(outcome.source).toBe('fallback');
    expect(outcome.error?.kind).toBe('invalid_response');
  });

  it('refuses a request with no eligible alternatives, because there is nothing to show', async () => {
    const client = clientWith(respondWith(200, DECISION_BODY));
    await expect(
      client.requestDecision({ placement: 'p', eligibleAlternatives: [] }),
    ).rejects.toBeInstanceOf(AdexError);
  });
});

describe('recordEvent', () => {
  it('generates an idempotent event id when none is supplied', async () => {
    const fetchImpl = vi.fn(() => Promise.resolve(jsonResponse(202, ACCEPTED_EVENT_BODY)));
    const client = clientWith(fetchImpl);
    await client.recordEvent({ type: 'click', decisionId: DECISION_BODY.decision_id });

    const [url, init] = callArgs(fetchImpl);
    expect(url).toBe(`${BASE_URL}/v1/events`);
    const body = JSON.parse(init.body as string) as { event_id: string; occurred_at: string };
    expect(body.event_id).toMatch(/^evt_[0-7][0-9A-HJKMNP-TV-Z]{25}$/);
    expect(body.occurred_at).toMatch(/Z$/);
  });

  it('reports a duplicate as a successful delivery, not as an error', async () => {
    const client = clientWith(respondWith(202, { ...ACCEPTED_EVENT_BODY, status: 'duplicate' }));
    const outcome = await client.recordEvent({
      type: 'click',
      eventId: 'evt_01JQZ8M0Q1R2S3T4V5W6X7Y8Z9',
    });
    expect(outcome).toEqual({
      eventId: 'evt_01JQZ8M0Q1R2S3T4V5W6X7Y8Z9',
      delivered: true,
      status: 'duplicate',
      error: null,
    });
  });

  it('reports a failed delivery without throwing, keeping the id for a retry', async () => {
    const client = clientWith(() => {
      throw new TypeError('offline');
    });
    const outcome = await client.recordEvent({
      type: 'click',
      eventId: 'evt_01JQZ8M0Q1R2S3T4V5W6X7Y8Z9',
    });
    expect(outcome.delivered).toBe(false);
    expect(outcome.status).toBe('failed');
    expect(outcome.eventId).toBe('evt_01JQZ8M0Q1R2S3T4V5W6X7Y8Z9');
    expect(outcome.error?.kind).toBe('network');
  });
});
