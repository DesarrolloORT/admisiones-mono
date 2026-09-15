import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { readExceptions } from './security-model.mjs';

const root = resolve(import.meta.dirname, '../..');
const controls = JSON.parse(readFileSync(process.argv[3] ?? resolve(root, 'security/asvs/controls/pilot.json'), 'utf8'));
const exceptions = readExceptions(process.argv[2] ?? resolve(root, 'security/asvs/exceptions'), controls);
const expired = exceptions.filter(item => Date.parse(`${item.expiresAt}T00:00:00Z`) <= Date.now()).length;
console.log(`Exceptions valid: ${exceptions.length}; active: ${exceptions.length - expired}; expired: ${expired}`);
