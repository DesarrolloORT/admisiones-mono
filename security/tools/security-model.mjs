import { createHash } from 'node:crypto';
import { appendFileSync, existsSync, readFileSync, readdirSync } from 'node:fs';
import { resolve } from 'node:path';

export const components = ['frontend', 'backend', 'system', 'infrastructure'];
export const environments = ['development', 'testing', 'preproduction', 'production'];
export const evidenceResults = ['PASS', 'FAIL', 'INFORMATIONAL'];
export const evidenceSources = ['sonar', 'zap', 'automated-test', 'static-rule', 'configuration', 'agent-review', 'human-review'];
export const severities = ['low', 'medium', 'high', 'critical'];

const requiredEvidence = ['controlId', 'application', 'component', 'repository', 'commitSha', 'source', 'environment', 'timestamp', 'result'];
const controlPattern = /^v5\.0\.0-\d+\.\d+\.\d+$/;
const shaPattern = /^[0-9a-f]{7,40}$/i;

export function stableFingerprint(value) {
  return createHash('sha256').update(JSON.stringify(value)).digest('hex');
}

export function evidenceFingerprint(item) {
  return stableFingerprint([
    item.controlId, item.commitSha, item.component, item.environment, item.source, item.tool ?? '', item.rule ?? '',
    item.file ?? '', item.line ?? '', item.test ?? '', item.result
  ]);
}

export function isIsoDateTime(value) {
  if (typeof value !== 'string' || !/^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d(?:\.\d+)?(?:Z|[+-]\d\d:\d\d)$/.test(value)) return false;
  const parsed = new Date(value);
  const [year, month, day] = value.slice(0, 10).split('-').map(Number);
  const calendarDate = new Date(Date.UTC(year, month - 1, day)).toISOString().slice(0, 10);
  return Number.isFinite(parsed.valueOf()) && calendarDate === value.slice(0, 10);
}

function isDateOnly(value) {
  if (typeof value !== 'string' || !/^\d{4}-\d\d-\d\d$/.test(value)) return false;
  const parsed = new Date(`${value}T00:00:00Z`);
  return Number.isFinite(parsed.valueOf()) && parsed.toISOString().slice(0, 10) === value;
}

export function validateEvidence(items, controls) {
  const controlsById = new Map(controls.map(control => [control.id, control]));
  const fingerprints = new Set();
  for (const [index, item] of items.entries()) {
    const label = `evidence item ${index + 1}`;
    const missing = requiredEvidence.filter(field => item[field] === undefined || item[field] === '');
    if (missing.length) throw new Error(`${label}: missing ${missing.join(', ')}`);
    if (!controlsById.has(item.controlId)) throw new Error(`${label}: unknown control ${item.controlId}`);
    if (item.application !== 'admisiones') throw new Error(`${label}: invalid application`);
    if (!components.includes(item.component)) throw new Error(`${label}: invalid component`);
    if (!controlsById.get(item.controlId).scope.includes(item.component)) throw new Error(`${label}: component outside control scope`);
    if (!environments.includes(item.environment)) throw new Error(`${label}: invalid environment`);
    if (!evidenceSources.includes(item.source)) throw new Error(`${label}: invalid source`);
    if (!evidenceResults.includes(item.result)) throw new Error(`${label}: invalid result`);
    if (!shaPattern.test(item.commitSha)) throw new Error(`${label}: invalid commitSha`);
    if (!isIsoDateTime(item.timestamp)) throw new Error(`${label}: invalid timestamp`);
    if (item.expiresAt !== undefined && !isIsoDateTime(item.expiresAt)) throw new Error(`${label}: invalid expiresAt`);
    if (item.expiresAt !== undefined && Date.parse(item.expiresAt) <= Date.parse(item.timestamp)) throw new Error(`${label}: expiresAt must be after timestamp`);
    if (item.confidence !== undefined && (!Number.isFinite(item.confidence) || item.confidence < 0 || item.confidence > 1)) throw new Error(`${label}: invalid confidence`);
    const expected = evidenceFingerprint(item);
    if (item.fingerprint !== undefined && item.fingerprint !== expected) throw new Error(`${label}: invalid fingerprint`);
    if (fingerprints.has(expected)) throw new Error(`${label}: duplicate fingerprint ${expected}`);
    fingerprints.add(expected);
    item.fingerprint = expected;
  }
  return items;
}

export function readEvidence(path, controls) {
  const items = readFileSync(path, 'utf8').split(/\r?\n/).filter(Boolean).map((line, index) => {
    try { return JSON.parse(line); } catch { throw new Error(`Invalid JSONL at ${path}:${index + 1}`); }
  });
  return validateEvidence(items, controls);
}

export function validateException(exception, name, controlIds) {
  for (const field of ['control', 'reason', 'risk', 'approvedBy', 'createdAt', 'expiresAt', 'ticket']) {
    if (!exception[field]) throw new Error(`${name}: missing ${field}`);
  }
  if (!controlPattern.test(exception.control) || !controlIds.has(exception.control)) throw new Error(`${name}: unknown control ${exception.control}`);
  if (!severities.includes(exception.risk)) throw new Error(`${name}: invalid risk`);
  if (!isDateOnly(exception.createdAt)) throw new Error(`${name}: invalid createdAt`);
  if (!isDateOnly(exception.expiresAt)) throw new Error(`${name}: invalid expiresAt`);
  if (Date.parse(`${exception.expiresAt}T00:00:00Z`) <= Date.parse(`${exception.createdAt}T00:00:00Z`)) throw new Error(`${name}: expiresAt must be after createdAt`);
  return exception;
}

export function readExceptions(directory, controls) {
  const ids = new Set(controls.map(control => control.id));
  return readdirSync(directory).filter(name => name.endsWith('.json')).sort().map(name => {
    let value;
    try { value = JSON.parse(readFileSync(resolve(directory, name), 'utf8')); } catch { throw new Error(`${name}: invalid JSON`); }
    return { ...validateException(value, name, ids), file: name };
  });
}

export function sameCommit(evidenceSha, evaluatedSha) {
  if (evidenceSha.length < 7 || evaluatedSha.length < 7) return false;
  return evidenceSha.startsWith(evaluatedSha) || evaluatedSha.startsWith(evidenceSha);
}

export function appendUniqueEvidence(path, items) {
  const existing = new Set();
  if (existsSync(path)) {
    for (const line of readFileSync(path, 'utf8').split(/\r?\n/).filter(Boolean)) existing.add(evidenceFingerprint(JSON.parse(line)));
  }
  let added = 0;
  for (const item of items) {
    item.fingerprint = evidenceFingerprint(item);
    if (existing.has(item.fingerprint)) continue;
    appendFileSync(path, `${JSON.stringify(item)}\n`);
    existing.add(item.fingerprint);
    added++;
  }
  return added;
}
