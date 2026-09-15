import { readdirSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const directory = resolve(import.meta.dirname, '../asvs/exceptions');
const now = Date.now();
let expired = 0;

for (const name of readdirSync(directory).filter(name => name.endsWith('.json'))) {
  const exception = JSON.parse(readFileSync(resolve(directory, name), 'utf8'));
  for (const field of ['control', 'reason', 'risk', 'approvedBy', 'createdAt', 'expiresAt', 'ticket']) {
    if (!exception[field]) throw new Error(`${name}: missing ${field}`);
  }
  if (!/^v5\.0\.0-\d+\.\d+\.\d+$/.test(exception.control)) throw new Error(`${name}: invalid control`);
  if (!Number.isFinite(Date.parse(exception.createdAt)) || !Number.isFinite(Date.parse(exception.expiresAt))) throw new Error(`${name}: invalid date`);
  if (Date.parse(exception.expiresAt) <= Date.parse(exception.createdAt)) throw new Error(`${name}: expiresAt must be after createdAt`);
  if (Date.parse(exception.expiresAt) <= now) expired++;
}

console.log(`Exceptions valid; expired: ${expired}`);
if (expired) process.exitCode = 2;

