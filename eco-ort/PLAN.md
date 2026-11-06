# Plan: docs de Admisiones dentro de ECO-ORT y hallazgos de sistema

Fecha: 2026-11-06 · Repos: `DesarrolloORT/ECO-ORT` (main @ `e35da3d`), `admisiones`, `api-admisiones`

El mismo plan está en dos repos, rama `feat/docs-admisiones`:

- ECO-ORT: `Documentos-proyecto/docs-admisiones/PLAN.md`, junto con `SEGUIMIENTO.md` (pendientes,
  decisiones y estado de cada fase);
- admisiones-mono: `eco-ort/PLAN.md`.

Si cambia, se actualizan los dos. Las rutas sin prefijo de repo son de ECO-ORT.

## 0. Objetivo

1. Desde **ECO-ORT > Sistemas > Admisiones**, ver el docs-site (Docusaurus) de Admisiones.
2. Desde ECO-ORT, crear un **hallazgo del sistema** Admisiones (no de un proyecto) que quede en una
   bandeja para que alguien de Desarrollo lo **tome**.
3. Cuando cambian `admisiones` o `api-admisiones`, un **workflow publica la versión nueva** de las docs
   en ECO-ORT y, si el cambio cierra un hallazgo, ECO-ORT lo registra en el hallazgo.

## 1. Punto de partida (verificado en el código)

| Hecho | Dónde | Qué implica |
| --- | --- | --- |
| `Finding.ProjectId` es obligatorio; no hay `SystemId` | `backend-dotnet/Models/Entities.cs:1083` | Hoy no existen hallazgos "del sistema": hay que cambiar el modelo |
| El hallazgo no tiene asignado; `ProjectTest` sí (`AsignadoUserId/AsignadoNombre`) | `Services/ProjectRequirementService.cs:646` | "Tomar" un hallazgo se copia de ese patrón |
| Circuito actual: AF crea (borrador) → AF aprueba → DEV lo ve y mueve estado | `Services/FindingService.cs:37-55,192` | Se conserva; solo cambia el contenedor |
| `SystemEntity` no tiene URL, repo ni docs; el código legible es `Tag` (`ADM`) | `Entities.cs:34-57` | Hace falta un registro "sitio de docs del sistema" |
| Carpetas y documentos de `/sistemas/:id` son un prototipo en `localStorage` | `system-content.store.ts:57` | El visor de docs es una pieza nueva, no se apoya en eso |
| Menú `sistemas`: ADMIN, COORD, AS (el AF no lo ve). Las keys de menú solo se aplican en el front | `MenuAccessService.cs:26-35` | El backend tiene que validar acceso al sistema por su cuenta |
| JWT en `localStorage`; no hay cookies | `core/auth/auth.service.ts:9` | Un iframe o una navegación no llevan auth: hace falta una cookie propia para las docs |
| nginx único dentro de la imagen del front; sin headers; `client_max_body_size` por defecto (1 MB) | `frontend-angular/nginx.conf` | `/docs/...` hoy cae en el SPA; hay que agregar `location` y subir el límite de la subida |
| ECO-ORT no tiene CI ni auth máquina a máquina en `main` | — | El CI de admisiones tiene que empujar hacia ECO-ORT con un token propio |
| La rama `MCP-ECO-AF` trae tokens `ecomcp_` (SHA-256, vencimiento, alcance por path) | `origin/MCP-ECO-AF:backend-dotnet/Auth/McpTokenAuth.cs` | Patrón a reutilizar para el token de publicación |
| Producción: `ecosistema-dsi.ort.edu.uy`, detrás de un Apache de la DSI (bloquea PATCH) | `deploy/README.md:113-142` | Verificar límite de tamaño de body del Apache |
| El docs-site existe solo en la rama `v1.0.0/fix/errores-af`; `src/css/` está sin commitear | `admisiones/docs-site` | Un build en CI hoy falla: primero hay que integrarlo |
| `web.config` del sitio: auth Windows + `frame-ancestors 'none'` + `X-Frame-Options: DENY` | `admisiones/docs-site/static/web.config` | Pensado para IIS; en ECO-ORT no aplica y se descarta al importar |
| El catálogo (`ui-catalog.json`) se genera con `../api-admisiones` al lado | `admisiones/docs-site/scripts/build-ui-catalog.mjs:8,79` | El CI tiene que hacer checkout de los dos repos lado a lado |
| Publicación IIS (P4) "bloqueado externo"; workflow de docs borrado en `df184e08` | `admisiones-mono/errores-af/SEGUIMIENTO-DOCS-SITE.md:19` | Hospedar en ECO-ORT elimina esa dependencia externa |
| Circuito E6 "Proponer cambio" con Microsoft List: sin implementar | `admisiones-mono/errores-af/PLAN-DOCS-SITE.md:229-277` | El hallazgo de ECO-ORT lo reemplaza con trazabilidad real |
| No hay `repository_dispatch` ni tokens entre repos para docs | `.github/workflows` de los 3 repos | Hay que crearlos |

## 2. Diseño

### 2.1 ECO-ORT hospeda el sitio, en el mismo origen

```
admisiones CI ──build──► site.zip + manifest.json ──POST (token de publicación)──► ECO-ORT API
                                                                                  │ valida, descomprime,
                                                                                  │ registra versión
                                                                                  ▼
                              ${ECOORT_DATA_ROOT}/docs-sites/admisiones/<versión>/  + enlace "actual"
                                                                                  │ montado read-only en el front
Navegador ─ /sistemas/:id/docs ─ iframe /docs/admisiones/... ─ nginx ─ auth_request ─► API (cookie de docs)
```

Por qué así:

- Saca de en medio el IIS con auth Windows (P4 bloqueado) y deja versiones y rollback en ECO-ORT.
- Mismo origen: el iframe funciona sin pelear con CSP, y ECO-ORT sabe qué pantalla está abierta para
  prellenar el hallazgo.

Descartado:

- **Enlazar al IIS** (lo que dice `admisiones/docs-site/README.md:3`): depende de P4, el `frame-ancestors 'none'`
  impide embeber y el hallazgo no recibe contexto.
- **Imagen Docker por sitio de docs** que el servidor descarga: ECO-ORT no tiene automatización en el
  servidor para hacer `pull`, y suma un contenedor por sistema.

**Autenticación del iframe**

1. El visor llama `POST /api/docs-sites/{slug}/sesion` con el JWT normal.
2. El backend valida que el usuario puede ver el sistema (alcance de área + rol, **en el backend**) y
   setea la cookie `ecoort_docs`: `HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/docs/`, 8 h. El valor
   es un JWT firmado con `aud=ecoort-docs`, `slug` y `exp`. El handler JWT principal rechaza ese
   audience, así la cookie no sirve contra `/api`.
3. nginx: `location /docs/` con `auth_request` a `/api/docs-sites/_auth`, que valida la cookie contra
   el slug de `X-Original-URI`. Si da 401, se sirve una página mínima que avisa al padre por
   `postMessage` para que pida sesión nueva y recargue.

**Riesgo del mismo origen (aceptarlo explícitamente).** El JS del sitio corre con el origen de
ECO-ORT y podría leer `localStorage['ecoort.auth']`. Mitigaciones:

- Solo publica el CI, con token, desde ramas protegidas con revisión obligatoria.
- La importación valida el paquete: lista blanca de extensiones, sin `web.config`, protección
  zip-slip, tamaño máximo.
- CSP en `/docs/`: `default-src 'self'; connect-src 'self'; img-src 'self' data:;
  frame-ancestors 'self'`, con `script-src 'self'` más el hash del script inline de tema de
  Docusaurus (probarlo; Mermaid puede pedir `style-src 'unsafe-inline'`).
- Arreglo definitivo: un subdominio propio para las docs (pedido a la DSI). El resto del diseño no
  cambia, solo el dominio de la cookie.

### 2.2 Hallazgo de sistema

**Modelo (migración en `AppDbContext`)**

- `Finding.ProjectId` pasa a nullable y se agrega `Finding.SystemId` nullable, con
  `CHECK (ProjectId IS NOT NULL OR SystemId IS NOT NULL)`.
  - Hallazgo de sistema = `ProjectId` null.
  - Backfill de los existentes: primer `ProjectSystem` del proyecto (misma regla que el "sistema
    principal", `Entities.cs:142-144`).
- `Origen`: `MANUAL` | `DOCS`.
- Contexto de docs: `DocsRuta`, `DocsScreenId`, `DocsTextoId` y `DocsVersionId` (FK a la versión
  publicada que se estaba mirando). Con eso el hallazgo muestra "visto en la versión del 06/11
  (admisiones@abc1234, api@def5678)" con enlace a la pantalla exacta.
- Asignación: `AsignadoUserId` y `AsignadoNombre`, copiando el patrón de `ProjectTest`.
- Código: los de sistema llevan el Tag (`ADM-HAL-001`); los de proyecto siguen `HAL-001`.
- `TagScope.HALLAZGOS_SISTEMA` con `ScopeRefId = SystemId`.
- `FindingEventType`: `ASIGNACION` y `PUBLICACION`.

**Circuito.** Es el mismo de hoy: crear (borrador) → aprobar → Desarrollo lo ve → **lo toma** → en curso
→ resuelto. Quién crea y quién aprueba los de sistema queda en la decisión D1.

**API**

- `GET/POST /api/systems/{systemId}/findings`.
- `PUT /api/findings/{id}/asignado`:
  - DEV: "Tomar" (a sí mismo) o "Soltar".
  - COORD/ADMIN: asignan a cualquiera.
- `FindingService.GetEntityAsync` valida el contenedor: proyecto (`ProjectAccessService`) **o** sistema
  (alcance de área). El resto de `/api/findings/{id}/...` sigue igual.

**Front**

- Visor: `/sistemas/:systemId/docs?ruta=/pantallas/auth-login/`, un iframe con encabezado que muestra:
  - versión publicada, fecha y commits;
  - el botón **Nuevo hallazgo**, prellenado con la pantalla abierta.

  El mismo componente se monta en `analisis-funcional/sistema/:systemId`, que hoy es un placeholder
  (`af-system-detail-page.component.html:26-28`).
- Desarrollo:
  - card "Hallazgos del sistema" en `devs/sistema/:systemId`;
  - ruta `devs/sistema/:systemId/hallazgos` que reutiliza `dev-findings-board` con `systemId`;
  - botón **Tomar** en `finding-card`, más el filtro "sin asignar / míos".
- Los chips de contexto de docs se agregan en `finding-detail`.

### 2.3 Docusaurus (admisiones)

- **Puente con ECO-ORT:** client module `src/ecoort-bridge.ts`. Solo se activa si el sitio está
  embebido y el origen padre está en `DOCS_EMBED_ORIGINS`, una variable de build.
  - En cada `onRouteDidUpdate` envía `{type:'ecoort:ruta', path, title, screenId}`.
  - En `CatalogExplorer.tsx` agrega en cada fila un botón **Reportar hallazgo**, visible solo
    embebido. El botón envía `{type:'ecoort:nuevo-hallazgo', screenId, textoId, textoActual, path}`.
  - El sitio **nunca** llama a la API: ECO-ORT abre su propio diálogo. Esto reemplaza E6 (Microsoft
    List).
- **Manifest:** `generate-deployment-metadata.mjs` suma:
  - `versions` (commits de front y back que ya trae `ui-catalog.json`);
  - `backendRef`, `runUrl` y `canal`;
  - `hallazgos`, la lista de la sección 2.4.
- **Build para ECO-ORT:** `DOCS_BASE_URL=/docs/admisiones/` y `DOCS_SITE_ORIGIN` según el canal.

### 2.4 CI: de los repos de Admisiones a ECO-ORT

**`api-admisiones/.github/workflows/docs-notify.yml`**

- Se dispara con push a `develop`/`main` filtrado por `paths` (`**/*.resx` y las carpetas de textos que
  lee `build-ui-catalog`).
- Ejecuta `gh api repos/DesarrolloORT/admisiones/dispatches` con
  `event_type=api-admisiones-docs` y `client_payload={canal, sha}`.
- Token: GitHub App de la org (preferido) o PAT fine-grained en `DOCS_DISPATCH_TOKEN`.

**`admisiones/.github/workflows/docs-eco-ort.yml`**

Triggers:

- push a `v*.*.*/develop` y `v*.*.*/main`, con `paths` (`docs/**`, `docs-site/**`, i18n `*-es.json`,
  `scripts/docs/**`);
- `repository_dispatch: [api-admisiones-docs]`;
- `workflow_dispatch` con input `canal`.

> `repository_dispatch` corre la versión del workflow de la **rama por defecto**: el job tiene que
> hacer checkout explícito de la rama de admisiones que corresponde al canal.

Mapeo de canales:

| Canal | admisiones | api-admisiones | ECO-ORT destino |
| --- | --- | --- | --- |
| `desa` | `v1.0.0/develop` | `develop` | desarrollo (`:4310`) |
| `prod` | `v1.0.0/main` | `main` | producción |

Jobs:

1. **build** (`ubuntu-latest`):
   - checkout de `admisiones/` y `api-admisiones/` lado a lado en `$GITHUB_WORKSPACE`, con un token de
     solo lectura;
   - `npm ci`, `docs:catalog` (regenera, no chequea), `check:knowledge`, build;
   - junta las referencias a hallazgos (sección 2.5);
   - arma el zip y el manifest y los sube con `upload-artifact`;
   - `concurrency` por canal.
2. **publish** (runner self-hosted dentro de la red ORT, p. ej. `development-server`; ver F0):
   - hace `POST $ECO_ORT_URL/api/integraciones/docs-sites/admisiones/versiones` multipart con
     `Authorization: Bearer $ECO_DOCS_TOKEN`;
   - usa los environments de GitHub `eco-ort-desa` y `eco-ort-prod`, con sus secretos; prod con
     aprobación.

**Endpoint de importación en ECO-ORT:** `POST /api/integraciones/docs-sites/{slug}/versiones`

- **Auth:** token de publicación `ecodocs_`, con hash guardado en `DocsSite` y alcance limitado a
  `/api/integraciones/docs-sites/{slug}/`. Reutiliza `Hash`/`NewToken` y el middleware de alcance de
  `McpTokenAuth`. ADMIN lo genera y rota desde Configuraciones (se ve una sola vez).
- También acepta **JWT de ADMIN**: es la carga manual desde la UI, el plan B mientras el CI no llega al
  servidor.
- **Valida:**
  - tamaño máximo (100 MB);
  - zip-slip y lista blanca de extensiones;
  - que existan `index.html` y `deployment.json`;
  - que slug y `baseUrl` coincidan;
  - y descarta `web.config`.
- Descomprime en `docs-sites/{slug}/{yyyyMMdd-HHmm}-{sha7}/` y cambia el enlace `actual` de forma
  atómica. Conserva las últimas 5 versiones y ADMIN puede volver a una anterior.
- Registra la fila `DocsSiteVersion` (commits, rama, canal, `runUrl`, fecha, tamaño) y procesa los
  hallazgos.

**Infra de ECO-ORT**

- Montaje `${ECOORT_DATA_ROOT}/docs-sites`: lectura y escritura en el backend, solo lectura en el
  front. En prod los contenedores corren con usuario `50001`, así que la carpeta tiene que ser suya.
- `nginx.conf`:
  - `location /docs/` (`auth_request`, `root` al montaje con rewrite a `<slug>/actual/`, CSP);
  - `client_max_body_size 100m` solo en `/api/integraciones/docs-sites/`.

### 2.5 Cierre de hallazgos por publicación

- **Convención:** en el título del PR o en el commit, `ECO: ADM-HAL-007`.
- **El workflow:**
  1. pide a ECO-ORT la versión actual del canal: `GET .../versiones/actual` devuelve los commits
     publicados;
  2. corre `git log` desde esos commits hasta HEAD en los dos repos;
  3. manda `hallazgos: [{codigo, repo, commit, prUrl}]` en el manifest.
- **ECO-ORT,** por cada código de **ese** sistema:
  - agrega un evento `PUBLICACION` en el chat del hallazgo: "Publicado en docs (canal desa/prod) — PR
    #123", con enlace a la pantalla;
  - según D4, mueve el estado.

## 3. Fases

| Fase | Repo | Entrega | Hecho cuando |
| --- | --- | --- | --- |
| **F0** Bloqueantes | todos | Cerrar el registro anónimo de ECO-ORT (ver §5). Confirmar red: runner self-hosted → ECO-ORT desa/prod y límite de body del Apache DSI. Decidir orden con la rama `MCP-ECO-AF` (toca `Program.cs`, `nginx.conf`, `docker-compose.yml`). Responder D1–D6 | Respuestas anotadas en `SEGUIMIENTO.md` de ECO-ORT |
| **F1** docs-site integrable | admisiones | Integrar `docs-site` a `v1.0.0/develop`; commitear `src/css`; corregir `editUrl` (`docs/catalogo/`); `ecoort-bridge`; botón en `CatalogExplorer`; manifest extendido | `DOCS_BASE_URL=/docs/admisiones/ npm run docs:build` pasa; el zip abre bajo esa ruta |
| **F2** Hosting y visor | ECO-ORT | Tablas `DocsSite` y `DocsSiteVersion`; importación (carga manual de ADMIN); cookie de sesión; `auth_request`; montajes; visor en Sistemas y en AF | En desa, ADMIN sube el zip de F1; un AS del área lo ve en `/sistemas/:id/docs`; alguien sin acceso recibe 401/404; un deep link abre la pantalla correcta |
| **F3** Hallazgo de sistema | ECO-ORT | Migración de `Finding`, endpoints, tablero de Desarrollo por sistema, Tomar/Soltar, alta desde el visor con contexto | Un AS crea `ADM-HAL-001` desde la pantalla de login; el AF lo aprueba; un DEV lo ve en `devs/sistema/:id/hallazgos`, lo toma y lo pasa a "En curso" |
| **F4** Publicación por CI | admisiones, api-admisiones, ECO-ORT | Token de publicación; `docs-eco-ort.yml`; `docs-notify.yml` | Un cambio de `.resx` en `api-admisiones/develop` termina en una versión nueva en ECO-ORT desa sin pasos manuales; el rollback desde la UI funciona |
| **F5** Cierre por publicación | admisiones, ECO-ORT | Referencias `ECO:`, evento `PUBLICACION`, regla de estado | Después de mergear un PR con `ECO: ADM-HAL-001` y publicar, el chat del hallazgo muestra el evento con el PR y el enlace a la pantalla |
| **F6** Opcional | ECO-ORT-MCP, mono | Herramientas MCP para Desarrollo (listar/tomar hallazgos); skill `/hallazgo ADM-HAL-007` que trae contexto y capturas; aviso en la bandeja (`InboxTask.SystemId`) al aprobar; PR abierto → "En curso"; subdominio de docs | — |

F1 y F2 van en paralelo. F3 no depende del CI. F4 depende de F0 (red).

## 4. Decisiones abiertas (con recomendación)

| # | Decisión | Recomendación |
| --- | --- | --- |
| D1 | ¿Quién crea y quién aprueba hallazgos de sistema? | Crean AF, AS, COORD y ADMIN; aprueban AF, AS y ADMIN; DEV ve solo los aprobados (igual que hoy) |
| D2 | El AF no ve el menú `sistemas` | Montar el mismo visor en `analisis-funcional/sistema/:id` en lugar de abrirle el menú |
| D3 | ¿Qué versión de las docs ve cada ECO-ORT? | ECO-ORT prod muestra `main` (lo que está en producción); desa muestra `develop` |
| D4 | ¿El CI puede resolver hallazgos? | En canal `prod`: evento y pase a "Resuelto". En `desa`: solo el evento. El AF verifica con el enlace a la pantalla |
| D5 | Riesgo de mismo origen (§2.1) | Aceptarlo con CSP y publicación solo por CI; pedir subdominio a la DSI en paralelo |
| D6 | Planes previos | P4 (IIS) y E6 (Microsoft List) de `admisiones-mono/errores-af/PLAN-DOCS-SITE.md` se reemplazan por este plan; actualizar `SEGUIMIENTO-DOCS-SITE.md` y `admisiones/docs-site/README.md`. Aclarar si `C:\GIT\frontend-docs` (Docusaurus con `proyectos/admisiones` y su propio deploy a IIS) queda en desuso |

## 5. Hallazgo de seguridad en ECO-ORT (independiente de este plan, urgente)

`POST /api/auth/register` es `[AllowAnonymous]` (`Controllers/AuthController.cs:17`) y toma el rol del
body (`Services/AuthService.cs:29`). No lo bloquean ni nginx ni `Program.cs`. Cualquiera que alcance la
API puede crearse un usuario **ADMIN**. Producción está publicada en `ecosistema-dsi.ort.edu.uy`.
Arreglo mínimo: exigir `[Authorize(Roles="ADMIN")]` o ignorar `Role` del body (siempre `USER`); revisar
los usuarios ADMIN existentes en prod.

## 6. Riesgos

- **Red:** si ningún runner llega a ECO-ORT, el plan B es la carga manual de F2 o que ECO-ORT haga
  `pull` del artifact. Esto último exige salida a internet y un token de GitHub en el servidor.
- **Catálogo:** el CI regenera `ui-catalog.json` sin commitearlo, así que la copia del repo puede quedar
  vieja y `docs:catalog:check` lo marcaría en PRs. Opción: un PR automático de sincronización.
- **Tamaño:** el sitio pesa unos 15 MB solo en PNG; con 5 versiones por canal, el disco crece unos
  150 MB por sistema.
- **Conflictos de merge:** con `MCP-ECO-AF` si se trabaja en paralelo; ver F0.
- **Desactualizados:**
  - `admisiones-mono/.claude/agents/errores-dev-docs.md` cita `build-error-catalog.mjs`, que no existe; el vigente es
    `build-ui-catalog.mjs`.
  - `.github/workflows/README.md` de api-admisiones es de otro proyecto.

## 7. Archivos que se tocan

**ECO-ORT**

- Backend:
  - `backend-dotnet/Models/Entities.cs` y `Enums.cs`, más la migración;
  - `Services/FindingService.cs`; `Services/DocsSiteService.cs` (nuevo);
  - `Controllers/FindingsController.cs`; `Controllers/DocsSitesController.cs` (nuevo);
  - `Program.cs` (esquema del token de publicación y su alcance).
- Infra:
  - `frontend-angular/nginx.conf`;
  - `docker-compose.yml` y `deploy/*/docker-compose.*.yml`.
- Front:
  - `app.routes.ts`;
  - `features/systems/**` (visor);
  - `features/devs/**` (tablero por sistema);
  - `shared/finding-card`, `shared/finding-detail`;
  - `data/api/findings.api.ts`; `data/api/docs-sites.api.ts` (nuevo).

**admisiones**

- `docs-site/docusaurus.config.ts`;
- `docs-site/src/ecoort-bridge.ts` (nuevo);
- `docs-site/src/components/CatalogExplorer.tsx`;
- `docs-site/scripts/generate-deployment-metadata.mjs`;
- `docs-site/src/css/`;
- `.github/workflows/docs-eco-ort.yml` (nuevo);
- `docs-site/README.md`.

**api-admisiones**

- `.github/workflows/docs-notify.yml` (nuevo).

**mono**

- `errores-af/PLAN-DOCS-SITE.md`, `errores-af/SEGUIMIENTO-DOCS-SITE.md`;
- `.claude/agents/errores-dev-docs.md`.
