import { readFileSync } from 'node:fs';
const rules = JSON.parse(readFileSync(new URL('../policies/change-impact.json', import.meta.url), 'utf8'));
const files = process.argv.slice(2).map(path => path.replaceAll('\\', '/'));
const affected = new Set(rules.filter(rule => files.some(file => file.startsWith(rule.pathPrefix))).flatMap(rule => rule.controls));
console.log([...affected].sort().join('\n'));

