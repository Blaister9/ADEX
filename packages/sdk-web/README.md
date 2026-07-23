# @adex/sdk-web

Framework-agnostic, dependency-free browser SDK for the ADEX decision API.

## Install and use

```ts
import { createAdexClient } from '@adex/sdk-web';

const adex = createAdexClient({
  apiKey: 'pk_live_...', // tenant publishable key; public by design
  baseUrl: 'https://api.your-adex-host.example',
  timeoutMs: 800,
});

const outcome = await adex.requestDecision({
  placement: 'homepage.primary-cta',
  eligibleAlternatives: ['request-callback', 'book-appointment'],
  context: { device_class: 'mobile', referrer_group: 'organic' },
});

render(outcome.alternativeKey); // your code decides how to render

await adex.recordEvent({
  type: 'click',
  decisionId: outcome.decisionId,
});
```

## Design rules

**It never decides.** Selection is authoritative on the server. Nothing in this
package can influence which alternative is chosen — a tenant tampering with the
browser must not be able to change the outcome (ADR-0003).

**It never rejects.** `requestDecision` and `recordEvent` always resolve. On any
failure — timeout, network error, HTTP error, malformed body — `requestDecision`
returns the *first eligible alternative* with `source: 'fallback'` and the
`error` attached. A slow or unavailable ADEX must never leave a blank region on
a tenant's page.

**It never writes HTML.** The SDK returns a key; the host page renders it. This
is what keeps ADEX from becoming an XSS delivery channel (threat T9 in the
threat model).

**It never sends cookies.** Requests use `credentials: 'omit'`.

**It has no runtime dependencies.** Including the ULID generator, which is
implemented in `src/ulid.ts` so that event identifiers can be produced
client-side — that is what makes retries safe, because the server deduplicates
on `event_id` (ADR-0012).

## What is deliberately missing in this version

- **Storage and consent.** This version never persists a subject identifier. If
  you have one, pass `subjectId`; otherwise decisions still work, without
  subject-level stability. The first-party storage, rotation and consent design
  is ADR-0008 and is implemented in roadmap task 004.
- **Offline queue.** A failed `recordEvent` is reported and dropped. Retrying is
  the caller's choice and is safe. A durable queue with `sendBeacon` on page
  unload is roadmap work.
- **Auto-instrumentation.** No automatic impression or click tracking; the host
  page emits what it means to emit.

## Local commands

```bash
pnpm --filter @adex/sdk-web build
pnpm --filter @adex/sdk-web typecheck
pnpm vitest run --project sdk-web
```
