import type * as Contract from '@adex/contracts';
import { describe, expect, it } from 'vitest';
import type * as Sdk from '../src/types';

/**
 * The SDK declares its wire types locally so the installable artefact stays
 * self-contained and dependency-free. These compile-time assignability checks
 * are what stop that copy from drifting: if `@adex/contracts` and `src/types.ts`
 * disagree, this file fails to type-check and CI fails.
 */
function assertMutuallyAssignable(): void {
  const decisionRequest: Contract.DecisionRequest = {} as Sdk.DecisionRequest;
  const decisionRequestBack: Sdk.DecisionRequest = decisionRequest;

  const decisionResponse: Contract.DecisionResponse = {} as Sdk.DecisionResponse;
  const decisionResponseBack: Sdk.DecisionResponse = decisionResponse;

  const eventRequest: Contract.EventRequest = {} as Sdk.EventRequest;
  const eventRequestBack: Sdk.EventRequest = eventRequest;

  const eventResponse: Contract.EventResponse = {} as Sdk.EventResponse;
  const eventResponseBack: Sdk.EventResponse = eventResponse;

  const problem: Contract.ProblemDetails = {} as Sdk.ProblemDetails;
  const problemBack: Sdk.ProblemDetails = problem;

  void decisionRequestBack;
  void decisionResponseBack;
  void eventRequestBack;
  void eventResponseBack;
  void problemBack;
}

describe('contract alignment', () => {
  it('keeps the SDK wire types assignable in both directions with @adex/contracts', () => {
    expect(() => {
      assertMutuallyAssignable();
    }).not.toThrow();
  });
});
