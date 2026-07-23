import { existsSync, readFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import SwaggerParser from '@apidevtools/swagger-parser';
import { parse as parseYaml } from 'yaml';
import { describe, expect, it } from 'vitest';
import { HEADERS, V1_PATHS } from '../src/index';

const OPENAPI_PATH = join(import.meta.dirname, '..', 'openapi', 'adex-public-v1.yaml');

interface OpenApiDocument {
  openapi: string;
  info: { title: string; version: string };
  paths: Record<string, Record<string, unknown>>;
  components: {
    securitySchemes: Record<string, { type: string; in?: string; name?: string }>;
  };
}

function loadRaw(): OpenApiDocument {
  return parseYaml(readFileSync(OPENAPI_PATH, 'utf8')) as OpenApiDocument;
}

/** Collect every `externalValue` in the document so example files cannot rot. */
function collectExternalValues(node: unknown, found: string[] = []): string[] {
  if (Array.isArray(node)) {
    for (const item of node) {
      collectExternalValues(item, found);
    }
  } else if (node !== null && typeof node === 'object') {
    for (const [key, value] of Object.entries(node)) {
      if (key === 'externalValue' && typeof value === 'string') {
        found.push(value);
      } else {
        collectExternalValues(value, found);
      }
    }
  }
  return found;
}

describe('OpenAPI document', () => {
  const raw = loadRaw();

  it('is OpenAPI 3.1', () => {
    expect(raw.openapi).toMatch(/^3\.1\./);
  });

  it('resolves every $ref, including the external JSON Schemas', async () => {
    // Dereferencing proves that schemas/, examples/ and the document agree.
    const dereferenced = (await SwaggerParser.dereference(OPENAPI_PATH)) as unknown as {
      paths: Record<string, unknown>;
    };
    expect(Object.keys(dereferenced.paths).length).toBeGreaterThan(0);
    expect(JSON.stringify(dereferenced)).not.toContain('$ref');
  });

  it('exposes exactly the paths the TypeScript contract constants declare', () => {
    expect(Object.keys(raw.paths).sort()).toEqual(
      [V1_PATHS.decisions, V1_PATHS.events, V1_PATHS.healthLive, V1_PATHS.healthReady].sort(),
    );
  });

  it('versions the public surface in the URL path', () => {
    expect(V1_PATHS.decisions.startsWith('/v1/')).toBe(true);
    expect(V1_PATHS.events.startsWith('/v1/')).toBe(true);
  });

  it('authenticates with the tenant-scoped publishable key header', () => {
    const scheme = raw.components.securitySchemes.PublishableApiKey;
    expect(scheme).toBeDefined();
    expect(scheme?.type).toBe('apiKey');
    expect(scheme?.in).toBe('header');
    expect(scheme?.name).toBe(HEADERS.apiKey);
  });

  it('leaves the health endpoints unauthenticated', () => {
    for (const path of [V1_PATHS.healthLive, V1_PATHS.healthReady]) {
      const operation = raw.paths[path]?.get as { security?: unknown[] } | undefined;
      expect(operation?.security, `${path} must opt out of the API key scheme`).toEqual([]);
    }
  });

  it('documents only failure modes the executable decision path implements', () => {
    const responses = (raw.paths[V1_PATHS.decisions]?.post as { responses: object }).responses;
    expect(Object.keys(responses).sort()).toEqual(['200', '400', '401', '409', '422', '503']);
  });

  it('documents only failure modes the executable event path implements', () => {
    const responses = (raw.paths[V1_PATHS.events]?.post as { responses: object }).responses;
    expect(Object.keys(responses).sort()).toEqual(['202', '400', '401', '422', '503']);
  });

  it('returns 202 from ingestion, because a replay is not an error', () => {
    const responses = (raw.paths[V1_PATHS.events]?.post as { responses: object }).responses;
    expect(Object.keys(responses)).toContain('202');
    expect(Object.keys(responses)).not.toContain('200');
  });

  it('references example files that exist on disk', () => {
    const externals = collectExternalValues(raw);
    expect(externals.length).toBeGreaterThanOrEqual(8);
    for (const relative of externals) {
      const absolute = resolve(dirname(OPENAPI_PATH), relative);
      expect(existsSync(absolute), `missing example file ${relative}`).toBe(true);
    }
  });

  it('shows both reference tenants, so no vertical is baked into the contract', () => {
    const externals = collectExternalValues(raw).join(' ');
    expect(externals).toContain('reference-services');
    expect(externals).toContain('reference-catalog');
  });
});
