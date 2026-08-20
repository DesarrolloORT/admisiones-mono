#!/usr/bin/env node
/**
 * Enlaza las tareas del project "Admisiones" con las ramas y deploys de este repo.
 *
 *   node scripts/task/task.js start <issue>
 *     Crea la rama sobre el head de develop, la pushea, mueve la tarea a
 *     "In progress Front" y hace checkout. <issue> acepta 137, api-admisiones#137
 *     o la URL completa del issue.
 *
 *   node scripts/task/task.js move --sha <sha> [--status Testing]
 *     Resuelve el PR que contiene el commit, saca el numero de issue del nombre
 *     de la rama y mueve la tarea. Lo usa dev-test-deploy.yml despues del deploy.
 *
 * El vinculo entre rama y tarea es el numero de issue al inicio del ultimo tramo
 * de la rama (v1.0.0/feat/137-slug). No se usa el "linked branch" nativo de GitHub:
 * createLinkedBranch modifica el issue y exige push en api-admisiones, donde el
 * equipo de front solo tiene triage. El numero en la rama es ademas la unica forma
 * de recuperar la tarea, porque la API va de issue a rama y nunca al revés.
 *
 * Auth: GITHUB_TOKEN, o `gh auth token` como fallback local.
 */

import { execFileSync } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const ROOT = resolve(fileURLToPath(new URL('../..', import.meta.url)));

const PROJECT_OWNER = 'DesarrolloORT';
const PROJECT_NUMBER = 24;
const TASKS_REPO = process.env.TASKS_REPO || 'DesarrolloORT/api-admisiones';
const TARGET_REPO = process.env.TARGET_REPO || 'DesarrolloORT/admisiones';
const STATUS_FIELD = 'Status';
const STATUS_IN_PROGRESS = 'In progress Front';
const STATUS_AFTER_DEPLOY = 'Testing';

const API = 'https://api.github.com';

function fail(message) {
  console.error(`error: ${message}`);
  process.exit(1);
}

function resolveToken() {
  if (process.env.GITHUB_TOKEN) return process.env.GITHUB_TOKEN;
  try {
    return execFileSync('gh', ['auth', 'token'], { encoding: 'utf8' }).trim();
  } catch {
    return fail('no hay token: definir GITHUB_TOKEN o autenticarse con `gh auth login`.');
  }
}

const TOKEN = resolveToken();

async function rest(route) {
  const res = await fetch(`${API}${route}`, {
    headers: { authorization: `Bearer ${TOKEN}`, accept: 'application/vnd.github+json' },
  });
  if (!res.ok) fail(`GET ${route} -> ${res.status} ${res.statusText}`);
  return res.json();
}

async function graphql(query, variables) {
  const res = await fetch(`${API}/graphql`, {
    method: 'POST',
    headers: { authorization: `Bearer ${TOKEN}`, 'content-type': 'application/json' },
    body: JSON.stringify({ query, variables }),
  });
  const body = await res.json().catch(() => null);
  if (!res.ok) fail(`graphql -> ${res.status} ${res.statusText}`);
  if (body?.errors?.length) throw new Error(body.errors.map(e => e.message).join('; '));
  return body.data;
}

/** Resuelve el project, el campo Status y sus opciones por nombre, no por id opaco. */
async function loadProject() {
  const data = await graphql(
    `
      query ($owner: String!, $number: Int!, $field: String!) {
        organization(login: $owner) {
          projectV2(number: $number) {
            id
            title
            field(name: $field) {
              ... on ProjectV2SingleSelectField {
                id
                name
                options {
                  id
                  name
                }
              }
            }
          }
        }
      }
    `,
    { owner: PROJECT_OWNER, number: PROJECT_NUMBER, field: STATUS_FIELD }
  );
  const project = data.organization?.projectV2;
  if (!project) {
    fail(
      `no se pudo leer el project ${PROJECT_OWNER}/${PROJECT_NUMBER}. Falta el scope "project" en el token?`
    );
  }
  if (!project.field) {
    fail(
      `el project "${project.title}" no tiene un campo single-select llamado "${STATUS_FIELD}".`
    );
  }
  return project;
}

function optionId(project, name) {
  const option = project.field.options.find(o => o.name === name);
  if (!option) {
    const names = project.field.options.map(o => o.name).join(', ');
    fail(`el campo ${STATUS_FIELD} no tiene la opcion "${name}". Opciones: ${names}`);
  }
  return option.id;
}

async function loadIssue(repo, number, projectId) {
  const [owner, name] = repo.split('/');
  const data = await graphql(
    `
      query ($owner: String!, $name: String!, $number: Int!) {
        repository(owner: $owner, name: $name) {
          issue(number: $number) {
            id
            number
            title
            url
            projectItems(first: 20) {
              nodes {
                id
                project {
                  id
                }
              }
            }
          }
        }
      }
    `,
    { owner, name, number }
  );
  const issue = data.repository?.issue;
  if (!issue) fail(`no existe el issue ${repo}#${number} o el token no tiene acceso.`);
  const item = issue.projectItems.nodes.find(node => node.project.id === projectId);
  return { issue, itemId: item?.id ?? null };
}

async function setStatus(project, itemId, statusName) {
  await graphql(
    `
      mutation ($project: ID!, $item: ID!, $field: ID!, $option: String!) {
        updateProjectV2ItemFieldValue(
          input: {
            projectId: $project
            itemId: $item
            fieldId: $field
            value: { singleSelectOptionId: $option }
          }
        ) {
          projectV2Item {
            id
          }
        }
      }
    `,
    {
      project: project.id,
      item: itemId,
      field: project.field.id,
      option: optionId(project, statusName),
    }
  );
}

// ---------- start ----------

function parseIssueRef(raw) {
  if (!raw) fail('falta el issue. Uso: npm run task:start -- 137');
  const url = raw.match(/github\.com\/([^/]+)\/([^/]+)\/issues\/(\d+)/);
  if (url) return { repo: `${url[1]}/${url[2]}`, number: Number(url[3]) };
  const qualified = raw.match(/^(?:([\w.-]+)\/)?([\w.-]+)#(\d+)$/);
  if (qualified) {
    const owner = qualified[1] || TASKS_REPO.split('/')[0];
    return { repo: `${owner}/${qualified[2]}`, number: Number(qualified[3]) };
  }
  if (/^#?\d+$/.test(raw)) return { repo: TASKS_REPO, number: Number(raw.replace('#', '')) };
  return fail(`no entiendo el issue "${raw}". Usar 137, api-admisiones#137 o la URL del issue.`);
}

function slugify(text) {
  return text
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
}

/** v1.0.0/feat/137-obtener-las-opciones: el numero abre el ultimo tramo. */
function branchNameFor(issue, version) {
  const conventional = issue.title.match(/^(\w+)(?:\([^)]*\))?:\s*(.+)$/);
  const type = conventional ? conventional[1].toLowerCase() : 'feat';
  const subject = conventional ? conventional[2] : issue.title;
  const slug =
    slugify(subject)
      .slice(0, 50)
      .replace(/-[^-]*$/, '') || 'task';
  return `v${version}/${type}/${issue.number}-${slug}`;
}

function git(...args) {
  return execFileSync('git', args, { encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] }).trim();
}

function branchExists(branch) {
  try {
    git('rev-parse', '--verify', `refs/heads/${branch}`);
    return true;
  } catch {
    return false;
  }
}

async function start(argv) {
  const dryRun = argv.includes('--dry-run');
  const { repo, number } = parseIssueRef(argv.find(arg => !arg.startsWith('--')));
  const pkg = JSON.parse(readFileSync(resolve(ROOT, 'package.json'), 'utf8'));
  const version = pkg.version.replace(/-.*$/, '');
  const base = process.env.DEVELOP_BRANCH || `v${version}/develop`;
  const [targetOwner, targetName] = TARGET_REPO.split('/');

  const project = await loadProject();
  const { issue, itemId } = await loadIssue(repo, number, project.id);
  const branch = branchNameFor(issue, version);

  const target = await graphql(
    `
      query ($owner: String!, $name: String!, $ref: String!) {
        repository(owner: $owner, name: $name) {
          id
          ref(qualifiedName: $ref) {
            target {
              oid
            }
          }
        }
      }
    `,
    { owner: targetOwner, name: targetName, ref: `refs/heads/${base}` }
  );
  const oid = target.repository.ref?.target?.oid;
  if (!oid)
    fail(`la rama base "${base}" no existe en ${TARGET_REPO}. Crearla antes de arrancar tareas.`);

  console.log(`tarea  ${issue.url}`);
  console.log(`       ${issue.title}`);

  if (dryRun) {
    console.log(`rama   ${branch}  (desde ${base} @ ${oid.slice(0, 8)})`);
    console.log(
      `status ${itemId ? STATUS_IN_PROGRESS : 'el issue no esta en el project; no se moveria'}`
    );
    console.log('dry-run: no se creo la rama ni se movio la tarea.');
    return;
  }

  // Con git plano y no con createLinkedBranch: esa mutacion modifica el issue y
  // exige push en api-admisiones, donde el equipo de front solo tiene triage.
  git('fetch', 'origin', base);
  if (branchExists(branch)) {
    git('checkout', branch);
    console.log(`rama   ${branch} (ya existia, se reutiliza)`);
  } else {
    git('checkout', '-b', branch, `origin/${base}`);
    git('push', '-u', 'origin', branch);
    console.log(`rama   ${branch} (creada desde ${base} @ ${oid.slice(0, 8)} y pusheada)`);
  }

  if (itemId) {
    await setStatus(project, itemId, STATUS_IN_PROGRESS);
    console.log(`status ${STATUS_IN_PROGRESS}`);
  } else {
    console.log(`aviso  el issue no esta en el project "${project.title}"; no se movio el status.`);
  }
}

// ---------- move ----------

function flag(argv, name) {
  const index = argv.indexOf(`--${name}`);
  return index >= 0 ? argv[index + 1] : undefined;
}

/** El numero abre el ultimo tramo, para no confundirlo con el 1.0.0 del prefijo. */
function issueNumberFromBranch(branch) {
  const last = branch.split('/').pop() || '';
  const match = last.match(/^(\d+)(?:-|$)/);
  return match ? Number(match[1]) : null;
}

async function branchFromSha(sha) {
  const pulls = await rest(`/repos/${TARGET_REPO}/commits/${sha}/pulls`);
  const merged = pulls.find(pull => pull.merged_at) || pulls[0];
  return merged?.head?.ref ?? null;
}

async function move(argv) {
  const status = flag(argv, 'status') || STATUS_AFTER_DEPLOY;
  const sha = flag(argv, 'sha');
  const branch = flag(argv, 'branch') || (sha ? await branchFromSha(sha) : null);

  if (!branch) {
    console.log('::warning::No se pudo determinar la rama del deploy; no se movio ninguna tarea.');
    return;
  }

  const number = issueNumberFromBranch(branch);
  if (!number) {
    console.log(
      `::warning::La rama "${branch}" no abre su ultimo tramo con el numero de issue; no se movio ninguna tarea.`
    );
    return;
  }

  const project = await loadProject();
  const { issue, itemId } = await loadIssue(TASKS_REPO, number, project.id);
  if (!itemId) {
    console.log(
      `::warning::${TASKS_REPO}#${number} no esta en el project "${project.title}"; no se movio.`
    );
    return;
  }

  await setStatus(project, itemId, status);
  console.log(`${TASKS_REPO}#${number} -> ${status}  (${issue.title})`);
}

// ---------- cli ----------

const [command, ...argv] = process.argv.slice(2);
const commands = { start, move };
if (!commands[command]) fail(`comando desconocido "${command ?? ''}". Usar: start | move`);
commands[command](argv).catch(error => fail(error.message));
