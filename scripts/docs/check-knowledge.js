import { execFileSync } from 'node:child_process';
import { existsSync, readdirSync, readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
function normalize(value) {
  return value.replaceAll('\\', '/').replace(/^\.\//, '');
}

function frontmatterValue(frontmatter, key) {
  return frontmatter.match(new RegExp(`^${key}:\\s*(.+)$`, 'm'))?.[1].trim() ?? null;
}

function frontmatterList(frontmatter, key) {
  const lines = frontmatter.split(/\r?\n/);
  const start = lines.findIndex(line => line.trim() === `${key}:`);
  if (start < 0) return [];

  const values = [];
  for (const line of lines.slice(start + 1)) {
    const item = line.match(/^\s+-\s+(.+?)\s*$/);
    if (!item) break;
    values.push(item[1].replace(/^(['"])(.*)\1$/, '$2'));
  }
  return values;
}

export function parseFlowMetadata(content, documentPath = 'unknown') {
  const match = content.match(/^---\r?\n([\s\S]*?)\r?\n---/);
  if (!match) throw new Error(`${documentPath}: missing frontmatter`);

  const businessId = frontmatterValue(match[1], 'businessId');
  const sourcePaths = frontmatterList(match[1], 'sourcePaths').map(normalize);
  if (!businessId) throw new Error(`${documentPath}: missing businessId`);
  if (!/^[a-z0-9.-]+$/.test(businessId)) {
    throw new Error(`${documentPath}: invalid businessId "${businessId}"`);
  }
  if (sourcePaths.length === 0) throw new Error(`${documentPath}: missing sourcePaths`);

  return { businessId, documentPath: normalize(documentPath), sourcePaths };
}

export function loadKnowledgeBase(root = ROOT) {
  const flowDirectory = path.join(root, 'docs', 'flujos');
  const flows = readdirSync(flowDirectory)
    .filter(file => file.endsWith('.md'))
    .map(file => {
      const documentPath = normalize(path.relative(root, path.join(flowDirectory, file)));
      return parseFlowMetadata(readFileSync(path.join(flowDirectory, file), 'utf8'), documentPath);
    });

  const ids = new Set();
  for (const flow of flows) {
    if (ids.has(flow.businessId)) throw new Error(`duplicate businessId: ${flow.businessId}`);
    ids.add(flow.businessId);

    for (const sourcePath of flow.sourcePaths) {
      const absolute = path.resolve(root, sourcePath);
      if (path.relative(root, absolute).startsWith('..')) {
        throw new Error(`${flow.businessId}: sourcePath leaves repository: ${sourcePath}`);
      }
      if (!existsSync(absolute)) {
        throw new Error(`${flow.businessId}: sourcePath does not exist: ${sourcePath}`);
      }
    }
  }

  return flows;
}

function isBusinessSource(file) {
  return /\.(?:html|ts)$/.test(file) && !file.endsWith('.spec.ts') && !file.includes('/generated/');
}

export function findMissingDocumentation(flows, changedFiles) {
  const changed = changedFiles.map(normalize);
  return flows.filter(flow => {
    const sourceChanged = changed.some(
      file => isBusinessSource(file) && flow.sourcePaths.some(source => file.startsWith(source))
    );
    return sourceChanged && !changed.includes(flow.documentPath);
  });
}

export function docsNoneReason(body = '') {
  const withoutComments = body.replace(/<!--[\s\S]*?-->/g, '');
  const reason = withoutComments.match(/^\s*docs-none:\s*(\S.*)$/im)?.[1].trim();
  if (!reason || /^<?(?:motivo|reason)>?$/i.test(reason) || /^(?:n\/?a|none|-)$/i.test(reason)) {
    return null;
  }
  return reason;
}

function changedFiles(base, head = 'HEAD') {
  return execFileSync('git', ['diff', '--name-only', `${base}...${head}`], {
    cwd: ROOT,
    encoding: 'utf8',
  })
    .split(/\r?\n/)
    .filter(Boolean);
}

export function checkDiff(flows, files, prBody = '') {
  const missing = findMissingDocumentation(flows, files);
  if (missing.length === 0) return { exempted: false, missing: [] };

  const reason = docsNoneReason(prBody);
  if (reason) return { exempted: true, missing, reason };

  throw new Error(
    `Business code changed without canonical documentation: ${missing
      .map(flow => `${flow.businessId} (${flow.documentPath})`)
      .join(', ')}. Update the page or add "docs-none: <reason>" to the PR body.`
  );
}

function argument(name) {
  const index = process.argv.indexOf(name);
  return index >= 0 ? process.argv[index + 1] : null;
}

function main() {
  const flows = loadKnowledgeBase();
  const base = argument('--base');

  if (base) {
    const result = checkDiff(
      flows,
      changedFiles(base, argument('--head') ?? 'HEAD'),
      process.env.PR_BODY
    );
    if (result.exempted) console.log(`Knowledge check exempted: ${result.reason}`);
  }

  console.log(`Knowledge check passed for ${flows.length} canonical flows.`);
}

const entrypoint = process.argv[1] ? pathToFileURL(path.resolve(process.argv[1])).href : '';
if (entrypoint === import.meta.url) {
  try {
    main();
  } catch (error) {
    console.error(error instanceof Error ? error.message : error);
    process.exitCode = 1;
  }
}
