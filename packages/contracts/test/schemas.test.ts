import { readFileSync, readdirSync } from 'node:fs';
import { basename, join } from 'node:path';
import Ajv2020, { type ValidateFunction } from 'ajv/dist/2020.js';
import addFormats from 'ajv-formats';
import { describe, expect, it } from 'vitest';

const SCHEMA_DIR = join(import.meta.dirname, '..', 'schemas');
const EXAMPLE_DIR = join(import.meta.dirname, '..', 'examples');
const INVALID_EXAMPLE_DIR = join(EXAMPLE_DIR, 'invalid');

function loadJson(path: string): unknown {
  return JSON.parse(readFileSync(path, 'utf8')) as unknown;
}

function schemaFiles(): string[] {
  return readdirSync(SCHEMA_DIR).filter((f) => f.endsWith('.schema.json'));
}

/** One Ajv instance holding every schema, so cross-file `$ref`s resolve by `$id`. */
function buildAjv(): Ajv2020 {
  const ajv = new Ajv2020({ strict: true, allErrors: true, allowUnionTypes: true });
  addFormats(ajv);
  for (const file of schemaFiles()) {
    ajv.addSchema(loadJson(join(SCHEMA_DIR, file)) as object);
  }
  return ajv;
}

/** `decision-request.reference-services.json` -> `decision-request.schema.json` */
function schemaIdForExample(fileName: string): string {
  const prefix = basename(fileName).split('.')[0] ?? '';
  return `https://contracts.adex.dev/v1/${prefix}.schema.json`;
}

function validatorFor(ajv: Ajv2020, fileName: string): ValidateFunction {
  const id = schemaIdForExample(fileName);
  const validate = ajv.getSchema(id);
  if (!validate) {
    throw new Error(`No schema registered for example ${fileName} (expected ${id})`);
  }
  return validate;
}

describe('JSON Schemas', () => {
  it('every schema compiles under Ajv strict mode', () => {
    const ajv = buildAjv();
    for (const file of schemaFiles()) {
      const id = `https://contracts.adex.dev/v1/${file}`;
      expect(ajv.getSchema(id), `schema ${file} must declare $id ${id}`).toBeDefined();
    }
  });

  it('declares the schemas the public contract depends on', () => {
    expect(schemaFiles().sort()).toEqual([
      'common.schema.json',
      'decision-request.schema.json',
      'decision-response.schema.json',
      'event-request.schema.json',
      'event-response.schema.json',
      'problem.schema.json',
    ]);
  });
});

describe('valid examples', () => {
  const ajv = buildAjv();
  const files = readdirSync(EXAMPLE_DIR).filter((f) => f.endsWith('.json'));

  it('there is at least one example per public payload', () => {
    expect(files.length).toBeGreaterThanOrEqual(8);
  });

  it('covers two unrelated reference tenants, proving domain independence', () => {
    expect(files.some((f) => f.includes('reference-services'))).toBe(true);
    expect(files.some((f) => f.includes('reference-catalog'))).toBe(true);
  });

  it.each(files)('%s validates against its schema', (file) => {
    const validate = validatorFor(ajv, file);
    const valid = validate(loadJson(join(EXAMPLE_DIR, file)));
    expect(valid, JSON.stringify(validate.errors, null, 2)).toBe(true);
  });
});

describe('invalid examples', () => {
  const ajv = buildAjv();
  const files = readdirSync(INVALID_EXAMPLE_DIR).filter((f) => f.endsWith('.json'));

  it('exists, so the schemas are proven to constrain and not merely to parse', () => {
    expect(files.length).toBeGreaterThanOrEqual(4);
  });

  it.each(files)('%s is rejected', (file) => {
    const validate = validatorFor(ajv, file);
    const valid = validate(loadJson(join(INVALID_EXAMPLE_DIR, file)));
    expect(valid, `${file} was accepted but must be rejected`).toBe(false);
  });
});

describe('contract invariants that the schemas must enforce', () => {
  const ajv = buildAjv();

  it('rejects a non-UTC timestamp offset instead of converting it', () => {
    const validate = validatorFor(ajv, 'event-request.x.json');
    expect(
      validate({
        event_id: 'evt_01JQZ8M0Q1R2S3T4V5W6X7Y8Z9',
        type: 'click',
        occurred_at: '2026-07-22T09:04:02+02:00',
      }),
    ).toBe(false);
  });

  it('rejects more than 50 eligible alternatives', () => {
    const validate = validatorFor(ajv, 'decision-request.x.json');
    const many = Array.from({ length: 51 }, (_, i) => ({ key: `variant-${String(i)}` }));
    expect(validate({ placement: 'p', eligible_alternatives: many })).toBe(false);
  });

  it('accepts a decision request with no subject and no context', () => {
    const validate = validatorFor(ajv, 'decision-request.x.json');
    const valid = validate({
      placement: 'homepage.primary-cta',
      eligible_alternatives: [{ key: 'variant-a' }],
    });
    expect(valid, JSON.stringify(validate.errors)).toBe(true);
  });

  it('accepts unknown fields in responses so the contract can grow', () => {
    const validate = validatorFor(ajv, 'decision-response.x.json');
    const valid = validate({
      decision_id: 'dec_01JQZ8K3N4P5R6S7T8V9W0X1Y2',
      alternative_key: 'variant-a',
      policy: { key: 'uniform-random', version: 1 },
      decided_at: '2026-07-22T14:03:11Z',
      field_added_in_a_later_release: true,
    });
    expect(valid, JSON.stringify(validate.errors)).toBe(true);
  });

  it('rejects unknown fields in requests so integration mistakes are visible', () => {
    const validate = validatorFor(ajv, 'decision-request.x.json');
    expect(
      validate({
        placement: 'homepage.primary-cta',
        eligible_alternatives: [{ key: 'variant-a' }],
        force_alternative: 'variant-a',
      }),
    ).toBe(false);
  });
});
