import { useCallback, useEffect, useState, type JSX } from 'react';
import { describeReadiness, probeReadiness, type HealthProbeResult } from './api';

export interface AppProps {
  readonly apiBaseUrl: string;
  readonly fetchImpl?: typeof fetch;
  /** Supplied by tests to render a deterministic state without network access. */
  readonly initialResult?: HealthProbeResult;
}

/**
 * Foundation shell. It deliberately contains no configuration or analytics
 * screens: those are roadmap task 006, and shipping empty screens now would
 * make the dashboard look more finished than it is.
 */
export function App({ apiBaseUrl, fetchImpl, initialResult }: AppProps): JSX.Element {
  const [result, setResult] = useState<HealthProbeResult | null>(initialResult ?? null);
  const [checking, setChecking] = useState(false);

  const check = useCallback(() => {
    setChecking(true);
    void probeReadiness({ baseUrl: apiBaseUrl, ...(fetchImpl ? { fetchImpl } : {}) })
      .then(setResult)
      .finally(() => {
        setChecking(false);
      });
  }, [apiBaseUrl, fetchImpl]);

  useEffect(() => {
    if (initialResult === undefined) {
      check();
    }
  }, [check, initialResult]);

  return (
    <main>
      <h1>ADEX dashboard</h1>
      <p>
        Administration and analytics shell. It consumes the public API contract only — it has no
        database access of its own.
      </p>

      <section aria-labelledby="api-status">
        <h2 id="api-status">API status</h2>
        <p data-testid="readiness">
          {result === null ? 'Checking…' : describeReadiness(result)}
        </p>
        <p>
          Endpoint: <code>{apiBaseUrl}/health/ready</code>
        </p>
        <button type="button" onClick={check} disabled={checking}>
          {checking ? 'Checking…' : 'Re-check'}
        </button>
      </section>

      <section aria-labelledby="next">
        <h2 id="next">Not built yet</h2>
        <ul>
          <li>Tenant, placement and alternative configuration (roadmap task 006)</li>
          <li>Policy binding and attribution-rule editing (roadmap task 006)</li>
          <li>Conversion, uncertainty and exploration analytics (roadmap task 006)</li>
        </ul>
      </section>
    </main>
  );
}
