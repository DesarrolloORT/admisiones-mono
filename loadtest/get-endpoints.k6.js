// k6 run loadtest/get-endpoints.k6.js
// Mide tiempo de respuesta de todos los GET de la API. Requiere una cuenta de prueba real
// (login está rate-limitado a 5 intentos/15min, por eso se loguea una sola vez en setup()).
//
// Variables de entorno:
//   BASE_URL        (default http://localhost:5000)
//   TIPO_DOCUMENTO  (ej. CI)
//   DOCUMENTO
//   PASSWORD
//   VUS             (default 5)
//   DURATION        (default 30s)
import http from 'k6/http';
import { check, group } from 'k6';
import { Trend } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

// Todos los GET conocidos (sin query string). Los que llevan params dinamicos (Comienzos,
// Turnos, Instituciones, Detalle) igual necesitan su Trend declarado aca aunque en un run
// puntual no se terminen llamando (setup() no encontro datos para resolver sus ids).
const ENDPOINT_LABELS = [
  'catalogs/countries-states-cities',
  'catalogs/initial-survey',
  'catalogs/degree-programs',
  'catalogs/intakes',
  'catalogs/shifts',
  'catalogs/banks',
  'catalogs/institutions',
  'enrollments/initial-survey',
  'enrollments/student-regulations',
  'enrollments/details',
  'person/details',
  'person/enrollments',
  'person/scholarships',
  'person/photo',
  'person/identity-document',
  'scholarships/enrollments',
];

// Nombres de metric en k6 solo aceptan [a-zA-Z0-9_]; "catalogs/degree-programs" pasa a
// "catalogs_degree_programs". Guardamos el mapeo inverso para mostrar el path legible en el reporte.
function metricName(label) {
  return label.replace(/\W/g, '_');
}
const labelByMetricName = Object.fromEntries(ENDPOINT_LABELS.map((label) => [metricName(label), label]));

// Los metrics (Trend incluido) deben crearse en el init context: no se puede hacer
// `new Trend(...)` recien al recibir el primer request, tiene que existir de antemano.
const trends = Object.fromEntries(ENDPOINT_LABELS.map((label) => [label, new Trend(metricName(label), true)]));

export const options = {
  vus: Number(__ENV.VUS || 5),
  duration: __ENV.DURATION || '30s',
  thresholds: {
    http_req_duration: ['p(95)<1000'],
  },
};

export function setup() {
  const loginRes = http.post(
    `${BASE_URL}/auth/login`,
    JSON.stringify({
      tipoDocumento: __ENV.TIPO_DOCUMENTO,
      documento: __ENV.DOCUMENTO,
      password: __ENV.PASSWORD,
    }),
    { headers: { 'Content-Type': 'application/json' } }
  );

  if (loginRes.status !== 200 || !loginRes.cookies['X-Access-Token']) {
    throw new Error(`Login fallo (status ${loginRes.status}): no se puede medir GETs autenticados.`);
  }
  const authHeaders = {
    headers: { Cookie: `X-Access-Token=${loginRes.cookies['X-Access-Token'][0].value}` },
  };

  // Params dinamicos: se resuelven una sola vez contra datos reales en vez de hardcodear ids.
  const paises = http.get(`${BASE_URL}/catalogs/countries-states-cities`).json('data') || [];
  const pais = paises[0];
  const codigoPais = pais?.codigoPais;
  const codigoEstado = pais?.estado?.[0]?.codigoEstado;

  const carreras = http.get(`${BASE_URL}/catalogs/degree-programs?academicOffer=UniversityDegree`, authHeaders).json('data') || [];
  const idCarrera = carreras[0]?.escuelas?.[0]?.productos?.[0]?.idProducto;

  let idProceso;
  if (idCarrera) {
    const comienzos =
      http.get(`${BASE_URL}/catalogs/intakes?degreeProgramId=${idCarrera}`, authHeaders).json('data') || [];
    idProceso = comienzos[0]?.idProceso;
  }

  const inscripciones = http.get(`${BASE_URL}/person/enrollments`, authHeaders).json('data') || [];
  const detalle = inscripciones[0];

  return {
    authHeaders,
    codigoPais,
    codigoEstado,
    idCarrera,
    idProceso,
    idProductoDetalle: detalle?.idProducto,
    idProcesoDetalle: detalle?.idProceso,
  };
}

export default function measureGets(data) {
  const { authHeaders } = data;

  // GETs sin parametros.
  const endpoints = [
    ['catalogs/countries-states-cities', {}], // publico
    ['catalogs/initial-survey', authHeaders],
    ['catalogs/degree-programs?academicOffer=UniversityDegree', authHeaders],
    ['catalogs/banks', authHeaders],
    ['enrollments/initial-survey', authHeaders],
    ['enrollments/student-regulations', authHeaders],
    ['person/details', authHeaders],
    ['person/enrollments', authHeaders],
    ['person/scholarships', authHeaders],
    ['person/photo', authHeaders],
    ['person/identity-document', authHeaders],
    ['scholarships/enrollments', authHeaders],
  ];

  // GETs con query params, solo si se pudo resolver un valor real en setup().
  if (data.idCarrera) {
    endpoints.push([`catalogs/intakes?degreeProgramId=${data.idCarrera}`, authHeaders]);
  }
  if (data.idCarrera && data.idProceso) {
    endpoints.push([`catalogs/shifts?degreeProgramId=${data.idCarrera}&admissionProcessId=${data.idProceso}`, authHeaders]);
  }
  if (data.codigoPais && data.codigoEstado) {
    endpoints.push([
      `catalogs/institutions?countryId=${data.codigoPais}&stateId=${data.codigoEstado}`,
      authHeaders,
    ]);
  }
  if (data.idProductoDetalle && data.idProcesoDetalle) {
    endpoints.push([
      `enrollments/details?productId=${data.idProductoDetalle}&admissionProcessId=${data.idProcesoDetalle}`,
      authHeaders,
    ]);
  }

  for (const [path, params] of endpoints) {
    const label = path.split('?')[0];
    group(label, () => {
      const res = http.get(`${BASE_URL}/${path}`, params);
      check(res, { 'status is 200': (r) => r.status === 200 });
      trends[label].add(res.timings.duration);
    });
  }
}

function rowColor(p95) {
  if (p95 < 200) return '#1a7f37';
  if (p95 < 1000) return '#9a6700';
  return '#cf222e';
}

// Genera una tabla en consola y un summary.html con el tiempo de respuesta por endpoint,
// ordenados de mas lento a mas rapido. labelByMetricName filtra los Trends propios y
// los muestra con su path legible en vez del nombre de metric sanitizado.
export function handleSummary(data) {
  const rows = Object.entries(data.metrics)
    .filter(([name]) => name in labelByMetricName)
    .map(([name, m]) => ({ name: labelByMetricName[name], avg: m.values.avg, p95: m.values['p(95)'], max: m.values.max }))
    .sort((a, b) => b.p95 - a.p95);

  const pad = (s, n) => String(s).padEnd(n);
  const fmt = (n) => `${n.toFixed(0)}ms`.padStart(9);
  let text = '\nTiempo de respuesta por endpoint, ordenado por p95 (mas lento primero):\n\n';
  text += `${pad('ENDPOINT', 45)}${'AVG'.padStart(9)}${'P95'.padStart(9)}${'MAX'.padStart(9)}\n`;
  rows.forEach((r) => {
    text += `${pad(r.name, 45)}${fmt(r.avg)}${fmt(r.p95)}${fmt(r.max)}\n`;
  });

  const html = `<!doctype html>
<html><head><meta charset="utf-8"><title>Tiempos de respuesta - api-admisiones</title>
<style>
  body { font-family: system-ui, sans-serif; margin: 2rem; color: #1f2328; }
  table { border-collapse: collapse; width: 100%; max-width: 800px; }
  th, td { padding: 0.5rem 0.75rem; text-align: right; border-bottom: 1px solid #d0d7de; }
  th:first-child, td:first-child { text-align: left; }
  th { background: #f6f8fa; }
</style></head>
<body>
  <h2>Tiempo de respuesta por endpoint (GET)</h2>
  <p>Generado ${new Date().toISOString()} contra <code>${BASE_URL}</code></p>
  <table>
    <thead><tr><th>Endpoint</th><th>avg</th><th>p95</th><th>max</th></tr></thead>
    <tbody>
      ${rows
        .map(
          (r) => `<tr>
        <td>${r.name}</td>
        <td>${r.avg.toFixed(0)}ms</td>
        <td style="color:${rowColor(r.p95)}; font-weight:600">${r.p95.toFixed(0)}ms</td>
        <td>${r.max.toFixed(0)}ms</td>
      </tr>`
        )
        .join('\n')}
    </tbody>
  </table>
</body></html>`;

  return {
    stdout: text,
    'loadtest/summary.html': html,
  };
}
