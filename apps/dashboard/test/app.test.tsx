import { renderToStaticMarkup } from 'react-dom/server';
import { describe, expect, it } from 'vitest';
import { App } from '../src/App';
import { describeReadiness, probeReadiness, statusTone, type HealthProbeResult } from '../src/api';

function jsonResponse(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json' },
  });
}

describe('readiness probe', () => {
  it('reads the public readiness endpoint, not the database', async () => {
    let requestedUrl = '';
    const result = await probeReadiness({
      baseUrl: 'http://localhost:5080',
      fetchImpl: ((url: string) => {
        requestedUrl = url;
        return Promise.resolve(jsonResponse(200, { status: 'healthy', checks: [] }));
      }) as unknown as typeof fetch,
    });
    expect(requestedUrl).toBe('http://localhost:5080/health/ready');
    expect(result.kind).toBe('ok');
  });

  it('reports an unreachable API instead of throwing', async () => {
    const result = await probeReadiness({
      baseUrl: 'http://localhost:5080',
      fetchImpl: () => {
        throw new TypeError('connection refused');
      },
    });
    expect(result).toEqual({ kind: 'unreachable', detail: 'The API did not respond.' });
  });

  it('rejects a payload that is not a health report', async () => {
    const result = await probeReadiness({
      baseUrl: 'http://localhost:5080',
      fetchImpl: () => Promise.resolve(jsonResponse(200, { nope: true })),
    });
    expect(result.kind).toBe('unreachable');
  });
});

describe('describeReadiness', () => {
  it('calls an optional dependency outage degraded, not unhealthy', () => {
    const result: HealthProbeResult = {
      kind: 'ok',
      report: {
        status: 'degraded',
        checks: [
          { name: 'postgres', status: 'healthy', required: true },
          { name: 'redis', status: 'unhealthy', required: false },
        ],
      },
    };
    expect(describeReadiness(result)).toBe('API degraded — optional dependency unavailable: redis');
  });

  it('calls a required dependency outage unhealthy', () => {
    const result: HealthProbeResult = {
      kind: 'ok',
      report: {
        status: 'unhealthy',
        checks: [{ name: 'postgres', status: 'unhealthy', required: true }],
      },
    };
    expect(describeReadiness(result)).toBe(
      'API unhealthy — required dependency unavailable: postgres',
    );
  });

  it('reports healthy when every check passes', () => {
    expect(
      describeReadiness({
        kind: 'ok',
        report: {
          status: 'healthy',
          checks: [{ name: 'postgres', status: 'healthy', required: true }],
        },
      }),
    ).toBe('API healthy');
  });
});

describe('statusTone', () => {
  it('maps every health status', () => {
    expect(statusTone('healthy')).toBe('good');
    expect(statusTone('degraded')).toBe('warn');
    expect(statusTone('unhealthy')).toBe('bad');
  });
});

describe('App shell', () => {
  it('renders the readiness summary it was given', () => {
    const markup = renderToStaticMarkup(
      <App
        apiBaseUrl="http://localhost:5080"
        initialResult={{ kind: 'ok', report: { status: 'healthy', checks: [] } }}
      />,
    );
    expect(markup).toContain('ADEX dashboard');
    expect(markup).toContain('API healthy');
    expect(markup).toContain('http://localhost:5080/health/ready');
  });

  it('states plainly which screens do not exist yet', () => {
    const markup = renderToStaticMarkup(
      <App
        apiBaseUrl="http://localhost:5080"
        initialResult={{ kind: 'unreachable', detail: 'The API did not respond.' }}
      />,
    );
    expect(markup).toContain('Not built yet');
    expect(markup).toContain('API unreachable');
  });
});
