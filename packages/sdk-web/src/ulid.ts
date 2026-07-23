/**
 * Minimal ULID generator.
 *
 * ADEX identifiers are Crockford base32 ULIDs so that they sort by creation
 * time and carry no device or user information (ADR-0008). The SDK generates
 * `event_id` client-side because that is what makes retries safe: the server
 * deduplicates on it (ADR-0012).
 *
 * Implemented here rather than pulled from npm to keep the SDK dependency-free.
 */

const ENCODING = '0123456789ABCDEFGHJKMNPQRSTVWXYZ';
const ENCODING_LENGTH = ENCODING.length;
const TIME_LENGTH = 10;
const RANDOM_LENGTH = 16;

export interface UlidOptions {
  /** Milliseconds since the Unix epoch. Injected so tests are deterministic. */
  now?: number;
  /** Random byte source. Injected so tests are deterministic. */
  randomBytes?: (length: number) => Uint8Array;
}

function defaultRandomBytes(length: number): Uint8Array {
  const bytes = new Uint8Array(length);
  const cryptoObject = globalThis.crypto as Crypto | undefined;
  if (cryptoObject?.getRandomValues) {
    cryptoObject.getRandomValues(bytes);
    return bytes;
  }
  throw new Error(
    'ADEX SDK requires a Web Crypto implementation (globalThis.crypto.getRandomValues).',
  );
}

function encodeTime(time: number): string {
  if (!Number.isFinite(time) || time < 0 || time > 0xffffffffffff) {
    throw new RangeError(`Timestamp out of ULID range: ${String(time)}`);
  }
  let remaining = Math.floor(time);
  let output = '';
  for (let index = 0; index < TIME_LENGTH; index++) {
    const modulo = remaining % ENCODING_LENGTH;
    output = ENCODING.charAt(modulo) + output;
    remaining = (remaining - modulo) / ENCODING_LENGTH;
  }
  return output;
}

function encodeRandom(randomBytes: (length: number) => Uint8Array): string {
  const bytes = randomBytes(RANDOM_LENGTH);
  let output = '';
  for (let index = 0; index < RANDOM_LENGTH; index++) {
    // Each byte contributes one base32 character; the top three bits are
    // discarded, which costs entropy per character but keeps the mapping
    // uniform over the alphabet.
    output += ENCODING.charAt((bytes[index] ?? 0) % ENCODING_LENGTH);
  }
  return output;
}

/** Returns a 26-character Crockford base32 ULID. */
export function ulid(options: UlidOptions = {}): string {
  const now = options.now ?? Date.now();
  const randomBytes = options.randomBytes ?? defaultRandomBytes;
  return encodeTime(now) + encodeRandom(randomBytes);
}

/** Returns a prefixed public identifier, e.g. `evt_01J...`. */
export function prefixedId(prefix: string, options: UlidOptions = {}): string {
  return prefix + ulid(options);
}
