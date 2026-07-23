import type { ProblemDetails } from './types';

export type AdexFailureKind =
  | 'network'
  | 'timeout'
  | 'http'
  | 'invalid_response'
  | 'invalid_argument';

/**
 * The SDK reports failures instead of throwing them at the host page. A tenant's
 * page must keep working when ADEX does not (see quality attribute 6).
 */
export class AdexError extends Error {
  readonly kind: AdexFailureKind;
  readonly status?: number;
  readonly problem?: ProblemDetails;
  readonly correlationId?: string;

  constructor(
    kind: AdexFailureKind,
    message: string,
    details: { status?: number; problem?: ProblemDetails; correlationId?: string } = {},
  ) {
    super(message);
    this.name = 'AdexError';
    this.kind = kind;
    if (details.status !== undefined) {
      this.status = details.status;
    }
    if (details.problem !== undefined) {
      this.problem = details.problem;
    }
    if (details.correlationId !== undefined) {
      this.correlationId = details.correlationId;
    }
  }
}
