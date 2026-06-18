// ai-toolkit:toolkit profile=agent-hooks path=scripts/ai-hooks/agent-hooks-shared.js
import { createHash } from 'node:crypto';
import {
  existsSync,
  mkdirSync,
  readdirSync,
  readFileSync,
  rmSync,
  statSync,
  writeFileSync,
} from 'node:fs';
import { tmpdir } from 'node:os';
import { basename, dirname, extname, join, relative, resolve } from 'node:path';

export const AGENT_HOOKS_STATE_ROOT = join(tmpdir(), 'ai-toolkit-agent-hooks');

const STATE_VERSION = 1;
const POLICY_PATH = '.github/ai-toolkit.policy.json';
const DEFAULT_DESIGN_SYSTEM_DOC_PATTERNS = [
  'docs/design-system/**/*.md',
  'docs/ui/**/*.md',
  'packages/*/README.md',
  'src/components/**/*.mdx',
  '**/*.stories.*',
];
const DEFAULT_PRE_COMMIT_SOURCE_PATTERNS = [
  '.husky/pre-commit',
  '.pre-commit-config.yaml',
  'lefthook.yml',
  'lefthook.yaml',
  'package.json',
];
const IGNORED_DIRS = new Set(['node_modules', '.git', 'dist']);
const UI_EXTENSIONS = new Set([
  '.tsx',
  '.jsx',
  '.vue',
  '.svelte',
  '.astro',
  '.html',
  '.css',
  '.scss',
  '.less',
]);
const UI_PROMPT_PATTERN =
  /\b(ui|ux|pantalla|screen|componente|component|layout|modal|dialog|drawer|form|formulario|tabla|table|card|navbar|header|footer|design system|design-system|a11y|accesibilidad)\b/i;
const SENSITIVE_PROMPT_PATTERN =
  /\b(auth|login|signup|token|secret|password|credential|permiso|role|rol|oauth|session|security|seguridad|csrf|xss|payment|pago)\b/i;
const CHECK_COMMAND_PATTERN =
  /\b(test|lint|eslint|prettier|typecheck|tsc|build|format|vitest|jest|playwright|cypress)\b/i;
const FILE_PATH_PATTERN =
  /(?:^|[\s`'"])(?:\.{0,2}\/)?(?:[\w.-]+\/)+[\w.-]+\.[A-Za-z0-9]{2,8}(?=$|[\s`'"])/;
const CONCRETE_SCOPE_PATTERN =
  /`[^`]+`|\.github\/workflows|\b(?:src|packages|docs|scripts|tests?|\.github|feature|flujo|pantalla|screen|componente|component|endpoint|route|hook|policy|manifest|readme|workflow|workflows|job|jobs|action|pipeline|ci|cd|ya?ml)\b/i;
const MAX_REPO_SUMMARY_LENGTH = 350;
const MAX_CONTEXT_LENGTH = 420;
const MAX_GOAL_LENGTH = 160;
const MAX_QUERY_LENGTH = 80;
const MAX_STEP_LENGTH = 120;
const MAX_FILES = 20;
const MAX_COMPACT_FILES = 4;
const MAX_SCOPE_HINT_FILES = 3;
const MAX_TARGETS = 250;
const MAX_SEARCH_FINGERPRINTS = 10;
const STATE_RETENTION_MS = 48 * 60 * 60 * 1000;
const PROFILE_ALIASES = {
  'preview-hooks': 'agent-hooks',
  'token-hooks': 'agent-hooks',
};
const REPO_SCALE_THRESHOLDS = {
  small: {
    maxFiles: 1000,
    wideScope: 20,
    wideEdit: 50,
  },
  medium: {
    maxFiles: 5000,
    wideScope: 40,
    wideEdit: 100,
  },
  large: {
    maxFiles: Number.POSITIVE_INFINITY,
    wideScope: 80,
    wideEdit: 200,
  },
};
const SECRET_PATTERNS = [
  /\bghp_[A-Za-z0-9]{20,}\b/g,
  /\bgithub_pat_[A-Za-z0-9_]{20,}\b/g,
  /\bsk-[A-Za-z0-9]{20,}\b/g,
  /\bAKIA[0-9A-Z]{16}\b/g,
  /\bAIza[0-9A-Za-z\-_]{35}\b/g,
];
const SECRET_ASSIGNMENT_PATTERN =
  /\b(token|secret|password|apikey|api_key|access[_-]?token)\b\s*[:=]\s*(['"]?)[^\s'"]{8,}\2/gi;

function normalizePath(filePath) {
  return String(filePath).replace(/\\/g, '/');
}

function uniq(items) {
  return [...new Set(items.map(item => normalizePath(item)).filter(Boolean))];
}

function truncate(value, limit) {
  if (!value) {
    return '';
  }

  const normalized = String(value).replace(/\s+/g, ' ').trim();
  if (normalized.length <= limit) {
    return normalized;
  }

  return `${normalized.slice(0, Math.max(0, limit - 1)).trimEnd()}...`;
}

function summarizeList(items, limit = MAX_SCOPE_HINT_FILES) {
  if (items.length === 0) {
    return '';
  }

  if (items.length <= limit) {
    return items.join(', ');
  }

  return `${items.slice(0, limit).join(', ')} (+${items.length - limit})`;
}

function redactSensitiveText(value) {
  if (!value) {
    return '';
  }

  let next = String(value);

  for (const pattern of SECRET_PATTERNS) {
    next = next.replace(pattern, '[redacted-secret]');
  }

  next = next.replace(SECRET_ASSIGNMENT_PATTERN, (_match, key) => `${key}=[redacted-secret]`);

  return next;
}

function sanitizeText(value, limit) {
  return truncate(redactSensitiveText(value), limit);
}

function readJsonIfExists(filePath) {
  if (!existsSync(filePath)) {
    return null;
  }

  try {
    return JSON.parse(readFileSync(filePath, 'utf8'));
  } catch {
    return null;
  }
}

function writeJson(filePath, value) {
  mkdirSync(dirname(filePath), { recursive: true });
  writeFileSync(filePath, `${JSON.stringify(value, null, 2)}\n`, 'utf8');
}

function normalizedTargets(items, limit = MAX_FILES) {
  return uniq(items).slice(0, limit);
}

function escapeRegExp(value) {
  return value.replace(/[|\\{}()[\]^$+?.]/g, '\\$&');
}

function globToRegExp(pattern) {
  const normalized = normalizePath(pattern).replace(/^\.\//, '');
  let output = '^';

  for (let index = 0; index < normalized.length; ) {
    const char = normalized[index];

    if (char === '*') {
      if (normalized[index + 1] === '*') {
        if (normalized[index + 2] === '/') {
          output += '(?:.*\\/)?';
          index += 3;
        } else {
          output += '.*';
          index += 2;
        }
        continue;
      }

      output += '[^/]*';
      index += 1;
      continue;
    }

    if (char === '?') {
      output += '[^/]';
      index += 1;
      continue;
    }

    output += escapeRegExp(char);
    index += 1;
  }

  output += '$';
  return new RegExp(output, 'i');
}

function listWorkspaceFiles(root, currentDir = root, files = []) {
  for (const entry of readdirSync(currentDir, { withFileTypes: true })) {
    if (entry.isDirectory()) {
      if (IGNORED_DIRS.has(entry.name)) {
        continue;
      }

      listWorkspaceFiles(root, join(currentDir, entry.name), files);
      continue;
    }

    files.push(normalizePath(relative(root, join(currentDir, entry.name))));
  }

  return files;
}

function resolvePatternMatches(files, patterns) {
  if (!patterns || patterns.length === 0) {
    return [];
  }

  const expressions = patterns.map(pattern => globToRegExp(pattern));
  return uniq(files.filter(file => expressions.some(expression => expression.test(file))));
}

function hasPackageJsonPreCommitConfig(root) {
  const pkg = readJsonIfExists(resolve(root, 'package.json'));
  if (!pkg || typeof pkg !== 'object') {
    return false;
  }

  const scripts = pkg.scripts && typeof pkg.scripts === 'object' ? pkg.scripts : {};

  return Boolean(
    pkg['lint-staged'] !== undefined ||
    pkg['simple-git-hooks'] !== undefined ||
    pkg.husky !== undefined ||
    scripts.precommit !== undefined ||
    scripts['pre-commit'] !== undefined
  );
}

function getAgentHooksMode(policy) {
  return policy?.agentHooks?.mode === 'fast' ? 'fast' : 'standard';
}

export function loadToolkitPolicy(root) {
  const absolutePath = resolve(root, ...POLICY_PATH.split('/'));
  if (!existsSync(absolutePath)) {
    return null;
  }

  try {
    return JSON.parse(readFileSync(absolutePath, 'utf8'));
  } catch {
    return null;
  }
}

export function getRepoScale(fileCount) {
  if (fileCount <= REPO_SCALE_THRESHOLDS.small.maxFiles) {
    return 'small';
  }

  if (fileCount <= REPO_SCALE_THRESHOLDS.medium.maxFiles) {
    return 'medium';
  }

  return 'large';
}

function getThresholds(scale) {
  return REPO_SCALE_THRESHOLDS[scale] ?? REPO_SCALE_THRESHOLDS.small;
}

export function resolveToolkitSignals(root) {
  const policy = loadToolkitPolicy(root);
  const files = listWorkspaceFiles(root);
  const designPatterns =
    Array.isArray(policy?.designSystem?.docs) && policy.designSystem.docs.length > 0
      ? policy.designSystem.docs
      : DEFAULT_DESIGN_SYSTEM_DOC_PATTERNS;
  const preCommitPatterns =
    Array.isArray(policy?.preCommit?.sources) && policy.preCommit.sources.length > 0
      ? policy.preCommit.sources
      : DEFAULT_PRE_COMMIT_SOURCE_PATTERNS;
  const designSystemDocs = resolvePatternMatches(files, designPatterns);
  const preCommitSources = resolvePatternMatches(
    files,
    preCommitPatterns.filter(pattern => pattern !== 'package.json' && pattern !== './package.json')
  );

  if (preCommitPatterns.includes('package.json') || preCommitPatterns.includes('./package.json')) {
    if (hasPackageJsonPreCommitConfig(root)) {
      preCommitSources.unshift('package.json');
    }
  }

  const mcp = readJsonIfExists(resolve(root, '.vscode', 'mcp.json'));
  const figmaMcpServer =
    typeof policy?.designSystem?.figmaMcpServer === 'string'
      ? policy.designSystem.figmaMcpServer
      : null;
  const workspaceMcpServers = new Set(Object.keys(mcp?.servers ?? {}));
  const repoScale = getRepoScale(files.length);

  return {
    policy,
    designSystemDocs,
    figmaMcpServer,
    figmaMcpServerAvailable: figmaMcpServer !== null && workspaceMcpServers.has(figmaMcpServer),
    preCommitSources: uniq(preCommitSources),
    mode: getAgentHooksMode(policy),
    repoFileCount: files.length,
    repoScale,
    thresholds: getThresholds(repoScale),
  };
}

export function buildDesignSystemMessage(signals) {
  if (signals.designSystemDocs.length > 0) {
    return `Design system docs: ${summarizeList(signals.designSystemDocs)}.`;
  }

  if (signals.figmaMcpServerAvailable) {
    return `No se resolvieron docs locales; usa Figma MCP '${signals.figmaMcpServer}' antes de inventar un componente.`;
  }

  return 'No se resolvieron docs ni Figma MCP; reutiliza primitives, tokens y patrones locales.';
}

export function buildPreCommitMessage(signals) {
  if (signals.preCommitSources.length > 0) {
    return `Pre-commit detectado en ${summarizeList(signals.preCommitSources)}. Anticipa formato, lint, typecheck, tests y scans relevantes antes de terminar.`;
  }

  return 'No se detectaron fuentes explicitas de pre-commit; igual evita deuda obvia de formato, lint, typecheck, tests y seguridad.';
}

export function buildUiRequirements() {
  return 'UI nueva: seguir WCAG 2.2 AA con semantica, teclado completo, foco visible, labels, estados, errores y contraste suficientes.';
}

export function buildSecurityRequirements() {
  return 'Codigo nuevo: secure-by-default con OWASP ASVS L2, sin secretos hardcodeados ni interpolaciones inseguras.';
}

export function analyzePrompt(prompt) {
  const isUi = UI_PROMPT_PATTERN.test(prompt);
  const isSensitive = SENSITIVE_PROMPT_PATTERN.test(prompt);

  return {
    isUi,
    isSensitive,
  };
}

function buildGuardrailClass(isUi, isSensitive) {
  if (isUi && isSensitive) {
    return 'ui-sensitive';
  }

  if (isUi) {
    return 'ui';
  }

  if (isSensitive) {
    return 'sensitive';
  }

  return 'general';
}

export function classifyPromptGuardrail(prompt) {
  const promptInfo = analyzePrompt(prompt);
  return buildGuardrailClass(promptInfo.isUi, promptInfo.isSensitive);
}

export function collectToolTargets(input) {
  const toolInput = input?.tool_input ?? input ?? {};
  const targets = [];

  const pushValue = value => {
    if (typeof value === 'string' && value.trim() !== '') {
      targets.push(normalizePath(value.trim()));
    }
  };

  const pushValues = value => {
    if (!Array.isArray(value)) {
      return;
    }

    for (const item of value) {
      pushValue(item);
    }
  };

  pushValues(toolInput.files);
  pushValues(toolInput.paths);
  pushValues(toolInput.targets);
  pushValue(toolInput.path);
  pushValue(toolInput.filePath);
  pushValue(toolInput.target);
  pushValue(toolInput.uri);

  return normalizedTargets(targets, MAX_TARGETS);
}

export function isUiFile(filePath) {
  return UI_EXTENSIONS.has(extname(filePath).toLowerCase());
}

function isSensitiveToolTarget(value) {
  return SENSITIVE_PROMPT_PATTERN.test(value);
}

export function classifyToolGuardrail(input) {
  const targets = collectToolTargets(input);
  const command = typeof input?.tool_input?.command === 'string' ? input.tool_input.command : '';
  const joinedTargets = targets.join(' ');
  const hasUiTargets = targets.some(target => isUiFile(target));
  const hasSensitiveTargets =
    isSensitiveToolTarget(joinedTargets) || isSensitiveToolTarget(command);

  return buildGuardrailClass(hasUiTargets, hasSensitiveTargets);
}

function normalizeProfileName(profile) {
  return PROFILE_ALIASES[profile] ?? profile;
}

function buildDefaultState(root, sessionId) {
  return {
    version: STATE_VERSION,
    sessionId,
    workspaceHash: getWorkspaceHash(root),
    workspaceName: basename(resolve(root)),
    startedAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    repoSummary: '',
    activeGoal: '',
    filesSeen: [],
    filesChanged: [],
    searchFingerprints: [],
    toolCounters: {
      search: 0,
      read: 0,
      edit: 0,
      command: 0,
      other: 0,
      globalSearches: 0,
      searchWarnings: 0,
      searchPrompts: 0,
      editPrompts: 0,
      checks: 0,
      compactions: 0,
      subagents: 0,
    },
    latestCompactSummary: null,
    repoScale: 'small',
    mode: 'standard',
    lastGuardrailClass: 'general',
  };
}

function normalizeCompactSummary(summary) {
  if (!summary || typeof summary !== 'object' || Array.isArray(summary)) {
    return null;
  }

  return {
    objetivo: sanitizeText(summary.objetivo, MAX_GOAL_LENGTH),
    archivosActivos: normalizedTargets(
      Array.isArray(summary.archivosActivos) ? summary.archivosActivos : [],
      MAX_COMPACT_FILES
    ),
    riesgosAbiertos: Array.isArray(summary.riesgosAbiertos)
      ? summary.riesgosAbiertos
          .filter(item => typeof item === 'string' && item.trim() !== '')
          .map(item => sanitizeText(item, MAX_STEP_LENGTH))
          .slice(0, 3)
      : [],
    siguientePaso: sanitizeText(summary.siguientePaso, MAX_STEP_LENGTH),
  };
}

function normalizeState(root, sessionId, value) {
  const base = buildDefaultState(root, sessionId);
  if (!value || typeof value !== 'object' || Array.isArray(value)) {
    return base;
  }

  const state = value;
  return {
    ...base,
    ...state,
    sessionId,
    workspaceHash: getWorkspaceHash(root),
    workspaceName: basename(resolve(root)),
    repoSummary:
      typeof state.repoSummary === 'string'
        ? sanitizeText(state.repoSummary, MAX_REPO_SUMMARY_LENGTH)
        : base.repoSummary,
    activeGoal:
      typeof state.activeGoal === 'string'
        ? sanitizeText(state.activeGoal, MAX_GOAL_LENGTH)
        : base.activeGoal,
    filesSeen: normalizedTargets(Array.isArray(state.filesSeen) ? state.filesSeen : []),
    filesChanged: normalizedTargets(Array.isArray(state.filesChanged) ? state.filesChanged : []),
    searchFingerprints: Array.isArray(state.searchFingerprints)
      ? state.searchFingerprints
          .filter(item => item && typeof item === 'object' && typeof item.fingerprint === 'string')
          .slice(0, MAX_SEARCH_FINGERPRINTS)
      : [],
    toolCounters: {
      ...base.toolCounters,
      ...(state.toolCounters && typeof state.toolCounters === 'object' ? state.toolCounters : {}),
    },
    latestCompactSummary: normalizeCompactSummary(state.latestCompactSummary),
    repoScale:
      state.repoScale === 'medium' || state.repoScale === 'large'
        ? state.repoScale
        : base.repoScale,
    mode: state.mode === 'fast' ? 'fast' : base.mode,
    lastGuardrailClass:
      typeof state.lastGuardrailClass === 'string' && state.lastGuardrailClass.trim() !== ''
        ? state.lastGuardrailClass
        : base.lastGuardrailClass,
  };
}

export function getWorkspaceHash(root) {
  return createHash('sha256')
    .update(normalizePath(resolve(root)))
    .digest('hex')
    .slice(0, 12);
}

function getWorkspaceStateDir(root) {
  return join(AGENT_HOOKS_STATE_ROOT, getWorkspaceHash(root));
}

export function getSessionStatePath(root, sessionId) {
  return join(getWorkspaceStateDir(root), 'sessions', `${sessionId}.json`);
}

export function getSessionSummaryPath(root, sessionId) {
  return join(getWorkspaceStateDir(root), 'summaries', `${sessionId}.json`);
}

export function cleanupExpiredState(now = Date.now()) {
  if (!existsSync(AGENT_HOOKS_STATE_ROOT)) {
    return;
  }

  for (const workspaceEntry of readdirSync(AGENT_HOOKS_STATE_ROOT, { withFileTypes: true })) {
    if (!workspaceEntry.isDirectory()) {
      continue;
    }

    const workspaceDir = join(AGENT_HOOKS_STATE_ROOT, workspaceEntry.name);
    for (const childName of ['sessions', 'summaries']) {
      const childDir = join(workspaceDir, childName);
      if (!existsSync(childDir)) {
        continue;
      }

      for (const fileEntry of readdirSync(childDir, { withFileTypes: true })) {
        if (!fileEntry.isFile()) {
          continue;
        }

        const filePath = join(childDir, fileEntry.name);
        const stats = statSync(filePath);
        if (now - stats.mtimeMs > STATE_RETENTION_MS) {
          rmSync(filePath, { force: true });
        }
      }
    }

    const remaining = readdirSync(workspaceDir, { withFileTypes: true }).some(entry => {
      if (!entry.isDirectory()) {
        return true;
      }

      const nestedDir = join(workspaceDir, entry.name);
      return readdirSync(nestedDir, { withFileTypes: true }).length > 0;
    });

    if (!remaining) {
      rmSync(workspaceDir, { recursive: true, force: true });
    }
  }
}

export function loadSessionState(root, sessionId) {
  return normalizeState(root, sessionId, readJsonIfExists(getSessionStatePath(root, sessionId)));
}

export function saveSessionState(root, sessionId, state) {
  const nextState = normalizeState(root, sessionId, {
    ...state,
    updatedAt: new Date().toISOString(),
  });
  writeJson(getSessionStatePath(root, sessionId), nextState);
  return nextState;
}

export function removeSessionState(root, sessionId) {
  rmSync(getSessionStatePath(root, sessionId), { force: true });
}

export function buildRepoSummary(root) {
  const packageJson = readJsonIfExists(resolve(root, 'package.json'));
  const manifest = readJsonIfExists(resolve(root, '.github', '.ai-toolkit', 'manifest.json'));
  const mcp = readJsonIfExists(resolve(root, '.vscode', 'mcp.json'));
  const name =
    typeof packageJson?.name === 'string' && packageJson.name.trim() !== ''
      ? packageJson.name.trim()
      : basename(resolve(root));
  const profiles =
    Array.isArray(manifest?.profiles) && manifest.profiles.length > 0
      ? uniq(manifest.profiles.map(profile => normalizeProfileName(profile)))
          .slice(0, 4)
          .join(', ')
      : 'custom';
  const mcpCount = Object.keys(mcp?.servers ?? {}).length;
  const hasPolicy = existsSync(resolve(root, '.github', 'ai-toolkit.policy.json')) ? 'yes' : 'no';

  return sanitizeText(
    `Workspace ${name} | profiles: ${profiles} | policy: ${hasPolicy} | MCP: ${mcpCount}.`,
    MAX_REPO_SUMMARY_LENGTH
  );
}

export function classifyToolUse(input) {
  const toolName = typeof input?.tool_name === 'string' ? input.tool_name.toLowerCase() : '';
  const toolInput = input?.tool_input ?? {};

  if (/search|grep|find|ripgrep|usages/.test(toolName)) {
    return 'search';
  }

  if (/read|open|view|fetch|cat/.test(toolName)) {
    return 'read';
  }

  if (/edit|write|create|update|insert|replace|applypatch/.test(toolName)) {
    return 'edit';
  }

  if (/shell|terminal|command|run|exec/.test(toolName)) {
    return 'command';
  }

  if (typeof toolInput.command === 'string' && toolInput.command.trim() !== '') {
    return 'command';
  }

  if (Array.isArray(toolInput.files) && toolInput.files.length > 0) {
    return 'edit';
  }

  if (
    typeof toolInput.query === 'string' ||
    typeof toolInput.pattern === 'string' ||
    typeof toolInput.search === 'string'
  ) {
    return 'search';
  }

  if (
    typeof toolInput.path === 'string' ||
    typeof toolInput.filePath === 'string' ||
    Array.isArray(toolInput.paths)
  ) {
    return 'read';
  }

  return 'other';
}

export function extractSearchQuery(input) {
  const toolInput = input?.tool_input ?? {};
  const candidates = [
    toolInput.query,
    toolInput.pattern,
    toolInput.search,
    toolInput.term,
    toolInput.text,
    toolInput.regex,
  ];

  return candidates.find(item => typeof item === 'string' && item.trim() !== '') ?? '';
}

export function hasUsefulSearchQuery(query) {
  const normalized = query.replace(/\s+/g, ' ').trim();
  return normalized.length >= 2 && /[A-Za-z0-9]/.test(normalized);
}

export function previewSearchFingerprint(state, input) {
  const query = truncate(
    extractSearchQuery(input).toLowerCase().replace(/\s+/g, ' ').trim(),
    MAX_QUERY_LENGTH
  );
  const targets = collectToolTargets(input);
  const isGlobal = targets.length === 0;
  const fingerprint =
    query === ''
      ? ''
      : `${isGlobal ? 'global' : 'scoped'}:${query}:${targets.slice(0, 5).join('|')}`;
  const existing = state.searchFingerprints.find(item => item.fingerprint === fingerprint);

  return {
    fingerprint,
    isGlobal,
    previousCount: existing?.count ?? 0,
    query,
    targets,
  };
}

export function recordSearchFingerprint(state, input) {
  const preview = previewSearchFingerprint(state, input);
  if (preview.fingerprint === '') {
    return preview;
  }

  const nextItems = [...state.searchFingerprints];
  const index = nextItems.findIndex(item => item.fingerprint === preview.fingerprint);
  const entry = {
    fingerprint: preview.fingerprint,
    query: preview.query,
    global: preview.isGlobal,
    count: preview.previousCount + 1,
    lastSeenAt: new Date().toISOString(),
  };

  if (index === -1) {
    nextItems.unshift(entry);
  } else {
    nextItems.splice(index, 1);
    nextItems.unshift(entry);
  }

  state.searchFingerprints = nextItems.slice(0, MAX_SEARCH_FINGERPRINTS);
  return {
    ...preview,
    nextCount: entry.count,
  };
}

export function updateSessionFiles(state, targets, kind) {
  state.filesSeen = normalizedTargets([...targets, ...state.filesSeen]);
  if (kind === 'edit') {
    state.filesChanged = normalizedTargets([...targets, ...state.filesChanged]);
  }
}

export function updateToolCounters(state, kind) {
  state.toolCounters[kind] = Number(state.toolCounters[kind] ?? 0) + 1;
}

export function noteCheckExecution(state, input) {
  const command = typeof input?.tool_input?.command === 'string' ? input.tool_input.command : '';
  if (CHECK_COMMAND_PATTERN.test(command)) {
    state.toolCounters.checks = Number(state.toolCounters.checks ?? 0) + 1;
  }
}

export function isConcretePrompt(prompt) {
  const normalized = prompt.replace(/\s+/g, ' ').trim();
  if (normalized === '') {
    return false;
  }

  return FILE_PATH_PATTERN.test(normalized) || CONCRETE_SCOPE_PATTERN.test(normalized);
}

export function setActiveGoal(state, prompt) {
  if (typeof prompt !== 'string' || prompt.trim() === '') {
    return;
  }

  state.activeGoal = sanitizeText(prompt, MAX_GOAL_LENGTH);
}

function buildRiskList(state) {
  const risks = [];

  if (!state.activeGoal) {
    risks.push('El objetivo sigue amplio.');
  }

  if (
    Number(state.toolCounters.searchWarnings ?? 0) > 0 ||
    Number(state.toolCounters.searchPrompts ?? 0) > 0
  ) {
    risks.push('Hubo busquedas globales repetidas; acotar antes de expandir.');
  }

  if (state.filesChanged.length > 10) {
    risks.push('Hay muchos archivos editados; validar impacto antes de expandir.');
  }

  if (risks.length === 0) {
    risks.push('Sin riesgos nuevos registrados.');
  }

  return risks.slice(0, 2);
}

export function buildCompactSummary(state) {
  const activeFiles = state.filesChanged.length > 0 ? state.filesChanged : state.filesSeen;
  const compactFiles = activeFiles.slice(0, MAX_COMPACT_FILES);
  const nextStep =
    state.filesChanged.length > 0
      ? `Validar ${state.filesChanged[0]} antes de abrir mas scope.`
      : compactFiles.length > 0
        ? `Continuar sobre ${compactFiles[0]} sin reexplorar el repo.`
        : 'Definir archivo, workflow o feature objetivo antes de seguir.';

  return normalizeCompactSummary({
    objetivo: state.activeGoal || 'No definido.',
    archivosActivos: compactFiles,
    riesgosAbiertos: buildRiskList(state),
    siguientePaso: nextStep,
  });
}

export function formatCompactSummary(summary) {
  const files =
    summary.archivosActivos.length > 0
      ? summarizeList(summary.archivosActivos, MAX_COMPACT_FILES)
      : 'sin archivos aun';
  const risks = summary.riesgosAbiertos.join(' ');
  return sanitizeText(
    `Objetivo: ${summary.objetivo} Archivos activos: ${files}. Riesgos: ${risks} Siguiente paso: ${summary.siguientePaso}`,
    MAX_CONTEXT_LENGTH
  );
}

export function buildSessionContext(state, extraHint = '') {
  const parts = [];

  if (state.activeGoal) {
    parts.push(`Objetivo activo: ${state.activeGoal}.`);
  }

  const activeFiles = state.filesChanged.length > 0 ? state.filesChanged : state.filesSeen;
  if (activeFiles.length > 0) {
    parts.push(`Archivos activos: ${summarizeList(activeFiles, MAX_SCOPE_HINT_FILES)}.`);
  }

  if (extraHint) {
    parts.push(extraHint);
  }

  return sanitizeText(parts.join(' '), MAX_CONTEXT_LENGTH);
}

export function buildSubagentContext(state) {
  const compactSummary = state.latestCompactSummary ?? buildCompactSummary(state);
  const extra = 'No reexplores todo el repo; reutiliza este estado y trabaja con scope minimo.';
  return sanitizeText(`${formatCompactSummary(compactSummary)} ${extra}`, MAX_CONTEXT_LENGTH);
}

function getSearchWarningLevel(previousCount, mode) {
  const occurrence = previousCount + 1;

  if (occurrence <= 1) {
    return 'none';
  }

  if (occurrence === 2) {
    return 'notice';
  }

  if (mode === 'fast') {
    if (occurrence <= 4) {
      return 'strong';
    }

    return 'ask';
  }

  if (occurrence === 3) {
    return 'strong';
  }

  return 'ask';
}

export function evaluatePreToolUse(state, input) {
  const kind = classifyToolUse(input);
  const targets = collectToolTargets(input);
  const reasons = [];
  const searchPreview = kind === 'search' ? previewSearchFingerprint(state, input) : null;
  const query = kind === 'search' ? extractSearchQuery(input) : '';
  const thresholds = getThresholds(state.repoScale ?? 'small');
  const warningLevel =
    kind === 'search' && searchPreview && searchPreview.isGlobal && searchPreview.fingerprint
      ? getSearchWarningLevel(searchPreview.previousCount, state.mode ?? 'standard')
      : 'none';

  if (targets.length > thresholds.wideScope) {
    reasons.push(`scope amplio (${targets.length} targets; repo ${state.repoScale})`);
  }

  if (kind === 'search' && !hasUsefulSearchQuery(query)) {
    reasons.push('busqueda sin query util');
  }

  if (
    kind === 'search' &&
    searchPreview &&
    searchPreview.isGlobal &&
    searchPreview.previousCount >= 1
  ) {
    reasons.push('busqueda global repetida');
  }

  if (kind === 'edit' && targets.length > thresholds.wideEdit) {
    reasons.push(`edicion masiva (${targets.length} archivos; repo ${state.repoScale})`);
  }

  return {
    kind,
    targets,
    reasons,
    isWide: reasons.length > 0,
    shouldAsk: (kind === 'edit' && targets.length > thresholds.wideEdit) || warningLevel === 'ask',
    searchPreview,
    warningLevel,
    warningOccurrence: kind === 'search' && searchPreview ? searchPreview.previousCount + 1 : 0,
  };
}

function buildWarningReason(evaluation) {
  if (evaluation.warningLevel === 'notice') {
    return 'Aviso: busqueda global repetida por segunda vez en la sesion.';
  }

  if (evaluation.warningLevel === 'strong') {
    return `Aviso fuerte: busqueda global repetida por ${evaluation.warningOccurrence} vez; acota path, workflow o feature antes de seguir.`;
  }

  if (evaluation.warningLevel === 'ask') {
    return `Busqueda global repetida por ${evaluation.warningOccurrence} vez; define scope antes de seguir.`;
  }

  return '';
}

export function buildPreToolResponse(state, evaluation, guardrailContext = '') {
  const hints = [];

  if (evaluation.kind === 'search' && evaluation.searchPreview?.isGlobal) {
    hints.push('Si ya conoces el area, acota path, workflow o feature antes de volver a buscar.');
  }

  if (evaluation.kind === 'edit' && evaluation.targets.length > 0) {
    hints.push(
      'Mantiene el cambio dentro del scope activo y evita editar mas archivos de los necesarios.'
    );
  }

  if (evaluation.reasons.includes('busqueda sin query util')) {
    hints.push('La query esta demasiado abierta; concretala antes de seguir.');
  }

  const sessionContext =
    evaluation.shouldAsk ||
    evaluation.warningLevel === 'notice' ||
    evaluation.warningLevel === 'strong' ||
    guardrailContext !== '' ||
    (evaluation.kind === 'edit' && evaluation.targets.length > 0)
      ? buildSessionContext(state, hints.join(' '))
      : '';
  const additionalContext = sanitizeText(
    [sessionContext, guardrailContext].filter(Boolean).join(' '),
    MAX_CONTEXT_LENGTH
  );
  const permissionDecision = evaluation.shouldAsk ? 'ask' : 'allow';
  const permissionDecisionReason = evaluation.shouldAsk
    ? buildWarningReason(evaluation) ||
      `Operacion amplia detectada: ${evaluation.reasons.join(', ')}.`
    : buildWarningReason(evaluation) || 'Scope minimo y estado de sesion reutilizado.';

  return {
    hookSpecificOutput: {
      hookEventName: 'PreToolUse',
      permissionDecision,
      permissionDecisionReason,
      ...(additionalContext ? { additionalContext } : {}),
    },
  };
}

export function buildInitialContext(state, signals) {
  return sanitizeText(
    [
      state.repoSummary,
      buildDesignSystemMessage(signals),
      buildUiRequirements(),
      buildSecurityRequirements(),
      buildPreCommitMessage(signals),
    ].join(' '),
    MAX_CONTEXT_LENGTH
  );
}

export function buildFullGuardrailContext(signals, guardrailClass) {
  const parts = [];

  if (guardrailClass === 'ui' || guardrailClass === 'ui-sensitive') {
    parts.push('Tarea UI detectada.');
    parts.push(buildDesignSystemMessage(signals));
    parts.push(buildUiRequirements());
  }

  if (guardrailClass === 'sensitive' || guardrailClass === 'ui-sensitive') {
    parts.push('Cambio sensible detectado.');
    parts.push(buildSecurityRequirements());
  }

  parts.push(buildPreCommitMessage(signals));

  return sanitizeText(parts.join(' '), MAX_CONTEXT_LENGTH);
}

export function buildSessionSummary(state) {
  return {
    version: STATE_VERSION,
    sessionId: state.sessionId,
    workspaceHash: state.workspaceHash,
    repoSummary: sanitizeText(state.repoSummary, MAX_REPO_SUMMARY_LENGTH),
    activeGoal: sanitizeText(state.activeGoal, MAX_GOAL_LENGTH),
    mode: state.mode ?? 'standard',
    repoScale: state.repoScale ?? 'small',
    metrics: {
      globalSearches: Number(state.toolCounters.globalSearches ?? 0),
      repeatedExplorationWarnings: Number(state.toolCounters.searchWarnings ?? 0),
      repeatedExplorationsAvoided: Number(state.toolCounters.searchPrompts ?? 0),
      uniqueFilesInspected: state.filesSeen.length,
      filesModified: state.filesChanged.length,
      subagentsStarted: Number(state.toolCounters.subagents ?? 0),
      compactions: Number(state.toolCounters.compactions ?? 0),
      checksExecuted: Number(state.toolCounters.checks ?? 0),
    },
    latestCompactSummary: normalizeCompactSummary(state.latestCompactSummary),
    finishedAt: new Date().toISOString(),
  };
}

export function saveSessionSummary(root, sessionId, state) {
  const summary = buildSessionSummary(state);
  writeJson(getSessionSummaryPath(root, sessionId), summary);
  return summary;
}
