#!/usr/bin/env node
/**
 * Fails when vertical-specific vocabulary appears in the ADEX core.
 *
 * `AGENTS.md` and `PROJECT_BRIEF.md` fix a hard product invariant: the engine
 * knows about tenants, placements, alternatives, policies, decisions, events and
 * rewards, and nothing about any particular industry. Industry meaning lives in
 * tenant configuration.
 *
 * That invariant is easy to state and easy to erode one helpful-looking field at
 * a time, so it is checked mechanically rather than trusted to review.
 *
 * Scope: production source only. Documentation may discuss the reference tenant
 * and its industry — explaining why the boundary exists is not violating it.
 */

import { readFileSync, readdirSync, statSync } from 'node:fs';
import { extname, join, relative, sep } from 'node:path';

const ROOT = process.cwd();

/** Directories whose contents are checked. */
const SCANNED = ['src', 'packages', 'apps', 'simulation', 'infra'];

/**
 * Skipped anywhere in the path.
 *
 * `examples` is skipped deliberately, not by oversight: contract examples are
 * *tenant configuration* for the reference tenants, and `PROJECT_BRIEF.md` says
 * a reference tenant's configuration may carry its industry's vocabulary. The
 * invariant is that the engine is neutral, not that no example may mention a
 * business. Schemas, the OpenAPI document and all production source are checked.
 */
const SKIPPED_SEGMENTS = new Set([
  'node_modules',
  'bin',
  'obj',
  'dist',
  'build',
  'coverage',
  '.venv',
  '__pycache__',
  '.ruff_cache',
  '.pytest_cache',
  'examples',
]);

const SCANNED_EXTENSIONS = new Set([
  '.cs',
  '.ts',
  '.tsx',
  '.js',
  '.mjs',
  '.py',
  '.sql',
  '.json',
  '.yaml',
  '.yml',
  '.html',
]);

/**
 * Vertical vocabulary. Each term is matched case-insensitively on a word
 * boundary. Verticals beyond the first reference tenant are included on purpose:
 * the rule is "no vertical", not "not this one".
 */
const FORBIDDEN = [
  // health / dental
  'dental',
  'dentist',
  'odontolog',
  'orthodont',
  'implantolog',
  'tooth',
  'teeth',
  'patient',
  'clinic',
  'diagnosis',
  'treatment',
  // retail / commerce specifics
  'sku',
  'cart',
  'checkout',
  'invoice',
  // education
  'student',
  'enrolment',
  'enrollment',
  'course',
  // real estate
  'property',
  'listing',
  'mortgage',
  'realtor',
];

/**
 * Narrow, justified exceptions. Each needs a reason: an unexplained allowance is
 * how a check like this stops meaning anything.
 */
const ALLOWED = [
  {
    // `properties` (event payload) and `propertyNames` (JSON Schema) both contain
    // "property"; neither is real-estate vocabulary.
    pattern: /\bpropert(y|ies)Names?\b|\bproperties\b|\bPropert(y|ies)\b/i,
    reason: 'event payload "properties" and the JSON Schema "propertyNames" keyword',
  },
  {
    pattern: /\bcourse\b(?=\s+of\s+action)/i,
    reason: 'the English phrase "course of action"',
  },
];

function* walk(directory) {
  let entries;
  try {
    entries = readdirSync(directory);
  } catch {
    return;
  }

  for (const entry of entries) {
    if (SKIPPED_SEGMENTS.has(entry)) {
      continue;
    }

    const full = join(directory, entry);
    if (statSync(full).isDirectory()) {
      yield* walk(full);
    } else if (SCANNED_EXTENSIONS.has(extname(full))) {
      yield full;
    }
  }
}

function isAllowed(line) {
  return ALLOWED.some(({ pattern }) => pattern.test(line));
}

const findings = [];

for (const directory of SCANNED) {
  for (const file of walk(join(ROOT, directory))) {
    const relativePath = relative(ROOT, file).split(sep).join('/');
    const lines = readFileSync(file, 'utf8').split(/\r?\n/);

    lines.forEach((line, index) => {
      if (isAllowed(line)) {
        return;
      }

      for (const term of FORBIDDEN) {
        if (new RegExp(`\\b${term}`, 'i').test(line)) {
          findings.push({ file: relativePath, line: index + 1, term, text: line.trim() });
        }
      }
    });
  }
}

if (findings.length > 0) {
  console.error('Vertical-specific vocabulary found in the ADEX core:\n');
  for (const finding of findings) {
    console.error(`  ${finding.file}:${finding.line}  [${finding.term}]  ${finding.text}`);
  }
  console.error(
    '\nThe core must stay domain-neutral (AGENTS.md invariant 1). Express this as tenant\n' +
      'configuration, or add a justified exception to scripts/check-domain-neutrality.mjs.',
  );
  process.exit(1);
}

console.log(`Domain neutrality: no vertical vocabulary found in ${SCANNED.join(', ')}.`);
