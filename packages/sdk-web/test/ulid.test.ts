import { describe, expect, it } from 'vitest';
import { prefixedId, ulid } from '../src/ulid';

const CROCKFORD = /^[0-7][0-9A-HJKMNP-TV-Z]{25}$/;

function fixedBytes(value: number): (length: number) => Uint8Array {
  return (length) => new Uint8Array(length).fill(value);
}

describe('ulid', () => {
  it('produces 26 Crockford base32 characters', () => {
    expect(ulid()).toMatch(CROCKFORD);
  });

  it('is deterministic when the clock and the entropy source are injected', () => {
    const options = { now: 1_774_180_991_482, randomBytes: fixedBytes(7) };
    expect(ulid(options)).toBe(ulid(options));
  });

  it('sorts lexicographically by creation time', () => {
    const earlier = ulid({ now: 1_774_180_991_000, randomBytes: fixedBytes(0) });
    const later = ulid({ now: 1_774_180_992_000, randomBytes: fixedBytes(0) });
    expect(earlier < later).toBe(true);
  });

  it('never repeats across many draws from the real entropy source', () => {
    const seen = new Set<string>();
    for (let i = 0; i < 5_000; i++) {
      seen.add(ulid());
    }
    expect(seen.size).toBe(5_000);
  });

  it('rejects timestamps outside the 48-bit ULID range', () => {
    expect(() => ulid({ now: -1 })).toThrow(RangeError);
    expect(() => ulid({ now: 2 ** 49 })).toThrow(RangeError);
  });

  it('prefixes public identifiers without changing the body grammar', () => {
    const id = prefixedId('evt_');
    expect(id.startsWith('evt_')).toBe(true);
    expect(id.slice('evt_'.length)).toMatch(CROCKFORD);
  });

  it('uses only the Crockford alphabet, so ambiguous characters cannot appear', () => {
    const body = ulid();
    for (const forbidden of ['I', 'L', 'O', 'U']) {
      expect(body.includes(forbidden)).toBe(false);
    }
  });
});
