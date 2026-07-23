import type { HealthReport, HealthStatus } from '@adex/contracts';

export interface ApiClientOptions {
  readonly baseUrl: string;
  readonly fetchImpl?: typeof fetch;
}

export type HealthProbeResult =
  | { readonly kind: 'ok'; readonly report: HealthReport }
  | { readonly kind: 'unreachable'; readonly detail: string };

function isHealthReport(value: unknown): value is HealthReport {
  if (value === null || typeof value !== 'object') {
    return false;
  }
  const status = (value as { status?: unknown }).status;
  return status === 'healthy' || status === 'degraded' || status === 'unhealthy';
}

/**
 * The dashboard consumes the same public API as any other client (ADR-0005);
 * it never reads the database directly.
 */
export async function probeReadiness(options: ApiClientOptions): Promise<HealthProbeResult> {
  const doFetch = options.fetchImpl ?? globalThis.fetch.bind(globalThis);
  try {
    const response = await doFetch(`${options.baseUrl.replace(/\/$/, '')}/health/ready`, {
      headers: { accept: 'application/json' },
    });
    const payload: unknown = await response.json();
    if (!isHealthReport(payload)) {
      return { kind: 'unreachable', detail: 'The API returned an unrecognised health report.' };
    }
    return { kind: 'ok', report: payload };
  } catch {
    return { kind: 'unreachable', detail: 'The API did not respond.' };
  }
}

/** Human-readable summary used by the shell; kept pure so it is trivially testable. */
export function describeReadiness(result: HealthProbeResult): string {
  if (result.kind === 'unreachable') {
    return `API unreachable — ${result.detail}`;
  }
  const failing = (result.report.checks ?? []).filter((check) => check.status !== 'healthy');
  if (failing.length === 0) {
    return 'API healthy';
  }
  const optionalOnly = failing.every((check) => !check.required);
  const names = failing.map((check) => check.name).join(', ');
  return optionalOnly
    ? `API degraded — optional dependency unavailable: ${names}`
    : `API unhealthy — required dependency unavailable: ${names}`;
}

export function statusTone(status: HealthStatus): 'good' | 'warn' | 'bad' {
  switch (status) {
    case 'healthy':
      return 'good';
    case 'degraded':
      return 'warn';
    case 'unhealthy':
      return 'bad';
  }
}
