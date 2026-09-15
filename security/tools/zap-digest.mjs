import { readFileSync, writeFileSync } from 'node:fs';

const RISK_ORDER = ['High', 'Medium', 'Low', 'Informational'];
const args = process.argv.slice(2);
const input = args.find(value => !value.startsWith('--'));
const option = name => { const index = args.indexOf(`--${name}`); return index === -1 ? undefined : args[index + 1]; };
if (!input) throw new Error('Usage: node security/tools/zap-digest.mjs <zap-report.json|zap-report.html> [--out digest.md] [--actions actions.json] [--mapping zap-mapping.json]');

const catalog = JSON.parse(readFileSync(option('actions') ?? new URL('../asvs/applicability/zap-actions.json', import.meta.url), 'utf8')).plugins;
const mapping = JSON.parse(readFileSync(option('mapping') ?? new URL('../asvs/applicability/zap-mapping.json', import.meta.url), 'utf8'));
const pluginByName = new Map(Object.entries(catalog).map(([id, entry]) => [entry.name.toLowerCase(), id]));

function decode(value) {
  return value.replace(/&(#x?[0-9a-f]+|[a-z]+);/gi, (match, code) => {
    if (code[0] === '#') return String.fromCodePoint(Number(code[1].toLowerCase() === 'x' ? `0x${code.slice(2)}` : code.slice(1)));
    return { amp: '&', lt: '<', gt: '>', quot: '"', apos: "'", nbsp: ' ' }[code.toLowerCase()] ?? match;
  });
}
const strip = value => decode(String(value).replace(/<[^>]+>/g, ' ')).replace(/\s+/g, ' ').trim();

function fromJson(raw) {
  let report;
  try { report = JSON.parse(raw); } catch { throw new Error('Reporte ZAP malformado: JSON inválido'); }
  if (!Array.isArray(report.site)) throw new Error('Reporte ZAP inválido: "site" debe ser un arreglo');
  const alerts = [];
  const urls = new Set();
  for (const site of report.site) {
    if (!Array.isArray(site.alerts)) throw new Error('Reporte ZAP inválido: "alerts" debe ser un arreglo');
    for (const alert of site.alerts) {
      const instances = (alert.instances ?? []).map(instance => instance.uri).filter(Boolean);
      for (const url of instances) urls.add(url);
      const [risk, confidence = ''] = String(alert.riskdesc ?? '').split(/\s*\(/);
      alerts.push({
        pluginid: String(alert.pluginid ?? ''),
        name: String(alert.alert ?? alert.name ?? 'Alerta sin nombre'),
        risk: risk.trim() || 'Informational',
        confidence: confidence.replace(')', '').trim(),
        count: Number(alert.count) || instances.length
      });
    }
  }
  return {
    format: 'JSON', title: report['@programName'] ?? 'Reporte ZAP', generatedAt: report['@generated'] ?? '',
    sites: report.site.map(site => site['@name']).filter(Boolean), alerts, urls: [...urls]
  };
}

function fromHtml(raw) {
  const table = raw.match(/<table class="alert-type-counts-table"[\s\S]*?<\/table>/)?.[0];
  if (!table) throw new Error('Reporte HTML de ZAP no reconocido: falta la tabla de alertas. Exporte el reporte en formato JSON.');
  const alerts = [];
  for (const row of table.match(/<tr[\s\S]*?<\/tr>/g) ?? []) {
    const cells = (row.match(/<td[\s\S]*?<\/td>/g) ?? []).map(strip);
    if (cells.length < 3 || !cells[0] || !RISK_ORDER.includes(cells[1])) continue;
    alerts.push({
      pluginid: pluginByName.get(cells[0].toLowerCase()) ?? '', name: cells[0], risk: cells[1],
      confidence: '', count: Number(cells[2].match(/\d+/)?.[0]) || 0
    });
  }
  const urls = [...new Set([...raw.matchAll(/class="request-method-n-url"[^>]*>([\s\S]*?)<\//g)]
    .map(match => strip(match[1]).replace(/^[A-Z]+\s+/, '')).filter(Boolean))];
  return {
    format: 'HTML', title: strip(raw.match(/<title>([\s\S]*?)<\/title>/)?.[1] ?? 'Reporte ZAP'),
    generatedAt: strip(raw.match(/<span>on ([\s\S]*?)<\/span>/)?.[1] ?? ''),
    sites: [...new Set((raw.match(/<(\w+) class="sites-list"[\s\S]*?<\/\1>/)?.[0]?.match(/<li[\s\S]*?<\/li>/g) ?? []).map(strip).filter(Boolean))],
    alerts, urls
  };
}

const report = input.toLowerCase().endsWith('.json') ? fromJson(readFileSync(input, 'utf8')) : fromHtml(readFileSync(input, 'utf8'));
const known = report.alerts.filter(alert => catalog[alert.pluginid]);
const unknown = report.alerts.filter(alert => !catalog[alert.pluginid]);
const byRisk = (a, b) => RISK_ORDER.indexOf(a.risk) - RISK_ORDER.indexOf(b.risk);
const select = verdict => known.filter(alert => catalog[alert.pluginid].verdict === verdict).sort(byRisk);
const instances = alerts => alerts.reduce((total, alert) => total + alert.count, 0);
const plural = (count, singular, many) => `${count} ${count === 1 ? singular : many}`;

const out = [];
const write = (...lines) => out.push(...lines, '');

write('# ZAP — informe para desarrollo');
write(
  `- **Escaneo:** ${report.title}${report.generatedAt ? ` · ${report.generatedAt}` : ''}`,
  `- **Objetivo:** ${report.sites.join(', ') || 'no declarado'}`,
  `- **Alertas:** ${plural(report.alerts.length, 'tipo', 'tipos')} / ${plural(instances(report.alerts), 'instancia', 'instancias')}`,
  `- **Fuente:** reporte ${report.format}${report.format === 'HTML' ? ' — sin `pluginid`, el mapeo ASVS requiere el export JSON' : ''}`
);

write('## Cobertura del escaneo');
write(
  `El reporte muestra alertas en **${plural(report.urls.length, 'URL', 'URLs')}**. Es una cota inferior del crawl: solo aparecen las URLs que dispararon alguna alerta.`,
  ...report.urls.slice(0, 15).map(url => `- \`${url}\``),
  report.urls.length > 15 ? `- … y ${report.urls.length - 15} más` : ''
);
if (report.urls.length < 10) {
  write('> **Atención:** con tan pocas URLs el escaneo no recorrió la aplicación. Sin AJAX Spider ni contexto autenticado, ZAP solo ve la pantalla inicial del SPA y sus archivos estáticos: **este reporte no dice nada sobre la API ni sobre los flujos con sesión.**');
}

const fixes = select('fix');
write('## Qué hay que hacer');
if (!fixes.length) write('Ninguna alerta accionable según el catálogo.');
const grouped = new Map();
for (const alert of fixes) {
  const entry = catalog[alert.pluginid];
  if (!grouped.has(entry.action)) grouped.set(entry.action, { where: entry.where, alerts: [], steps: new Set() });
  const group = grouped.get(entry.action);
  group.alerts.push(alert);
  group.steps.add(entry.fix);
}
let position = 0;
for (const [action, group] of grouped) {
  write(`### ${++position}. ${action}`);
  write(
    `**Dónde:** ${group.where}`, '',
    `**Resuelve ${plural(group.alerts.length, 'alerta', 'alertas')} / ${plural(instances(group.alerts), 'instancia', 'instancias')}:**`,
    ...group.alerts.map(alert => `- ${alert.name} — ${alert.risk} (${alert.count})`), '',
    '**Cómo:**', ...[...group.steps].map(step => `- ${step}`)
  );
}

if (unknown.length) {
  write('## Sin clasificar');
  write(
    'Estas alertas no están en `security/asvs/applicability/zap-actions.json`. Requieren una decisión humana y luego una entrada en el catálogo.',
    '',
    ...unknown.sort(byRisk).map(alert => `- **${alert.name}** — ${alert.risk} (${alert.count})${alert.pluginid ? ` · pluginid \`${alert.pluginid}\`` : ''}`)
  );
}

for (const [verdict, heading, intro] of [
  ['likely-false-positive', 'Probables falsos positivos', 'Verificar antes de escalar. No abrir ticket sin confirmar.'],
  ['informational', 'Ruido e informativos', 'No requieren cambios en la aplicación.']
]) {
  const selected = select(verdict);
  if (!selected.length) continue;
  write(`## ${heading}`);
  write(intro);
  for (const alert of selected) {
    write(`### ${alert.name} — ${alert.risk}${alert.confidence ? ` / confianza ${alert.confidence}` : ''} (${alert.count})`);
    write(catalog[alert.pluginid].fix);
  }
}

write('## Impacto en el baseline ASVS');
const mapped = report.alerts.filter(alert => (mapping[alert.pluginid] ?? []).length);
write(
  mapped.length
    ? `\`import-zap.mjs\` registrará evidencia FAIL para:\n${mapped.map(alert => `- ${alert.name} → ${mapping[alert.pluginid].join(', ')}`).join('\n')}`
    : 'Ninguna alerta de este reporte está mapeada a un control del piloto en `zap-mapping.json`: no se genera evidencia ASVS.',
  '',
  'Cero findings no demuestra cumplimiento: ZAP solo aporta evidencia negativa sobre la superficie que alcanzó a recorrer.'
);

const digest = `${out.join('\n').replace(/\n{3,}/g, '\n\n').trim()}\n`;
const target = option('out');
if (target) { writeFileSync(target, digest); console.log(`Digest escrito en ${target}`); } else process.stdout.write(digest);
