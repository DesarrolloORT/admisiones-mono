# Auditoría de integración Angular ↔ ASP.NET Core: CSRF, cookies, CORS, sesión y XSS

Fecha: 2026-10-22. Revisión estática de código. No se modificó código, no se ejecutaron builds, tests, scans ni tráfico contra ningún ambiente.

## Alcance y evidencia

| Componente | Repositorio / ref evaluada | Abreviatura en este documento |
|---|---|---|
| API | `api-admisiones` @ `develop` `35dede20` | `WA/` = `WebApiAdmisiones/WebApiAdmisiones/`, `AL/` = `WebApiAdmisiones/AppLogic/` |
| Front | `admisiones` @ `origin/v1.0.0/develop` `039bce88` | `FE/` = `src/app/`, `FER/` = raíz del repo |
| Angular runtime | `@angular/common` 22.0.7 instalado en el clon local (el lockfile resuelve 22.2.1) | `node_modules/@angular/common/fesm2022/_module-chunk.mjs` |

Los subtrees `admisiones/` y `api-admisiones/` de este monorepo están en otras refs (`888ecfc8`, `aadcda6f`); las líneas citadas corresponden a las refs de la tabla.

**Fuera del alcance verificable desde el código**, y por eso marcado *a confirmar* donde importa:

- Key Vault (`Authentication:CookieDomain`, `Jwt:*`).
- Azure App Configuration del front (`API_URL` de testing, preprod y prod; CSP efectiva).
- `ASPNETCORE_ENVIRONMENT` real de cada host.
- Topología de proxy/IIS.

**Relación con fichas ASVS previas:**

- La conclusión de [3.5.1](../asvs/planning/revisiones/v5.0.0-3.5.1.md) y [3.5.2](../asvs/planning/revisiones/v5.0.0-3.5.2.md) se apoyaba en que "no hay `FormData`/`multipart`". Esa premisa **dejó de ser cierta** con ADR-019 (`90ccee3d`, 2026-10-09), posterior a esas fichas (2026-09-17). Ver V-01.
- [7.4.1](../asvs/planning/revisiones/v5.0.0-7.4.1.md) está desactualizada en un punto: el logout ahora sí revoca el refresh token.

## Estado de corrección (2026-10-22, sin commitear)

Cada hallazgo corregido tiene una prueba que primero falló contra el código sin tocar, demostrando la
vulnerabilidad, y después pasó con el fix. El detalle está en `api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`.

| ID | Estado | Prueba |
|---|---|---|
| V-01 | Corregido: `UseOriginCheck` responde 403 a métodos no seguros desde un `Origin` fuera del ambiente | `UnitTesting/Security/CsrfOriginValidationTests.cs` (HTTP real) |
| V-02 | Corregido: `Cors:AllowedOrigins` por `appsettings.{env}.json` | `CorsPolicyByEnvironmentTests.cs` |
| V-03 | **Pendiente de definición de negocio** (cómo reautenticar) | `PersonCredentialChangeTests.cs`, en `Skip`: sin `Skip` falla |
| V-04 | Corregido en parte. Cambiar la contraseña invalida el refresh, y el refresh no extiende la sesión ni renueva a quien el login rechaza. El access token sigue sin poder revocarse (vive 15 min) | `SessionRevocationTests.cs`, `RefreshSessionRulesTests.cs` |
| V-05 | Corregido: `Authentication:CookieSameSite`, `Strict` por defecto; `None` solo en `Development`/`LocalHost` | `CookieSameSiteTests.cs` |
| V-06 | Sin cambios: vive en Azure App Configuration e IIS | — |
| V-07 | Corregido en el front: `clearSession()` borra el contexto de inscripción | `auth-session.spec.ts` |

---

## Resumen ejecutivo

**El mecanismo anti-CSRF no existe en ninguno de sus cuatro eslabones.** Ni la API emite token, ni Angular lo recibe o lo envía, ni la API valida token ni `Origin`. Hoy la única defensa es indirecta:

1. El preflight CORS que disparan los cuerpos JSON.
2. `SameSite=Strict`, que se aplica **solo** si `ASPNETCORE_ENVIRONMENT` es `Production` o `Preproduction`.

Ese esquema tiene huecos concretos:

| ID | Severidad | Hallazgo |
|---|---|---|
| V-01 | **Alta** | 7 acciones que cambian estado aceptan *simple requests* (sin preflight): 4 uploads `multipart/form-data` autenticados, `scholarships/applications/accept` sin body, `auth/logout` y `auth/refresh-token`. Son explotables desde **cualquier sitio** donde la cookie es `SameSite=None` (todo ambiente no productivo), y en producción desde **cualquier `https://*.ort.edu.uy`**, que es *same-site*. |
| V-02 | **Alta** | La política CORS es la misma en todos los ambientes. En producción acepta **con credenciales** a `admisionesdesa*`, `admisionestesting*`, `admisionespreprod` y `http://localhost:4200/5001`. Como esos orígenes son *same-site* con producción, un JS en cualquiera de ellos lee y escribe con la sesión productiva del usuario. |
| V-03 | **Alta** (en cadena) | `PUT person/details` cambia **email y documento** sin reautenticación ni verificación. Ese email recibe el 2FA y el link de recuperación. Cadena completa: XSS u origen confiado comprometido → cambiar email → `recover-password` → toma de cuenta. |
| V-04 | Media | Sesiones no revocables: el cambio de contraseña no revoca nada; el refresh es deslizante, sin vida absoluta, y no revalida el estado de la cuenta; el access token no se puede revocar. |
| V-05 | Media | `SameSite` falla abierto: si `ASPNETCORE_ENVIRONMENT` falta, ASP.NET Core corre como `Production`, pero la cookie sale `SameSite=None`. |
| V-06 | Baja | La CSP del front usa un nonce fijo, documentado en el repo, junto con `'strict-dynamic'`. |
| V-07 | Baja | El logout no limpia `sessionStorage` (IDs de inscripción y telemetría). |

**Controles verificados sin hallazgo**, para no reportar falsos positivos:

- El JWT nunca llega a JavaScript: va en cookie HttpOnly, no aparece en el body y no se guarda en storage.
- No hay XSS por sinks en Angular.
- No hay IDOR en los casos de uso auditados.
- No hay GET con efectos de estado.
- El refresh del front no entra en bucle.
- Los guards no son la única barrera.
- Todas las mutaciones JSON y los DELETE requieren preflight.
- El login no admite CSRF.

---

## 1. Arquitectura HTTP real

### Hallazgos

| Pregunta | Respuesta | Evidencia |
|---|---|---|
| URL pública del front | Producción `https://admisiones.ort.edu.uy`; otros ambientes `https://admisiones{desa,desa2,testing,testing2,preprod}.ort.edu.uy` | `WA/appsettings.Production.json:61-62` (`CrearPasswordUrl`); allowlist CORS `WA/Extensions/ServiceCollectionExtensions.cs:178-188` |
| URL pública de la API | Desa: `https://apiadmisionesdesa.ort.edu.uy`. Testing, preprod y prod no están en el repo (Azure App Config `frontend:admisiones:api_base`) | `FER/src/environments/environment.generated.ts:4` (archivo viejo, sin uso, pero es el único valor versionado); `FER/docs/CONFIGURACION_AMBIENTE_AZURE.md:163-165` |
| ¿Comparten esquema, host y puerto? | **No.** El host es distinto (`admisiones*` frente a `apiadmisiones*`). Son orígenes distintos dentro del mismo sitio registrable `ort.edu.uy` (`edu.uy` es sufijo público) | idem |
| ¿Cómo arma Angular la URL? | Siempre **absoluta**: `new URL(path, environment.API_URL)`. Con un `API_URL` absoluto que tenga prefijo (`https://h/api`), **el prefijo se pierde** (`/auth/login` reemplaza el path). Solo un `API_URL` relativo (`/api`) conserva el prefijo, a través del `catch` | `FE/shared/api/core/api-http-client.ts:53, 207-213` |
| ¿Reverse proxy o `/api`? | No hay configuración de proxy en ningún repo. El front es estático en IIS (`web.config.template`), con un único rewrite SPA → `/index.html` | `FER/src/web.config.template:30-36`; sin nginx, Dockerfile de front ni `proxy.conf` |
| ¿Same-origin o cross-origin? | **Cross-origin, same-site.** El propio código lo asume: el comentario CORS dice "Front y API están en orígenes distintos" | `WA/Extensions/ServiceCollectionExtensions.cs:203-204` |
| ¿Redirecciones entre dominios? | Solo hay una: el pago externo hace un `form POST` a una URL que devuelve el backend, con chequeo de esquema pero sin allowlist de host | `FE/features/enrollments/services/external-payment-submitter.ts:14-53` |
| ¿La autenticación depende de orígenes externos? | reCAPTCHA v3 (Google) en login, recuperación y registro. LDAP (SOAP) del lado servidor. No hay OAuth/OIDC | `FE/core/interceptors/http.ts:24-42`; `WA/Security/Captcha/RequireCaptchaAttribute.cs:45,72` |

**Conclusión sobre el "mismo dominio".** Compartir `ort.edu.uy` (*same-site*) **no** es compartir origen. Tiene tres consecuencias directas:

1. La protección XSRF nativa de Angular **no aplica**: solo agrega el header si el origen del request coincide con el de la página (verificado en `@angular/common` 22.0.7, `_module-chunk.mjs:1918-1944`: `if (locationOrigin !== requestOrigin) return next(req);`).
2. `SameSite=Strict` **no protege** contra páginas de otros subdominios `*.ort.edu.uy`, porque para el navegador son *same-site*.
3. Las requests legítimas llegan con `Sec-Fetch-Site: same-site`, el mismo valor que las de un subdominio atacante. Solo el header `Origin` exacto permite distinguirlas.

Si la intención es desplegar *same-origin* (front y API en `https://admisiones.ort.edu.uy`, API bajo `/api` detrás de un proxy):

- `API_URL` tiene que ser **relativo** (`/api`), por el punto 2 de la tabla.
- CORS deja de ser necesario en producción.

**A confirmar.** El workflow de release del front sincroniza el ambiente `desa` antes del build de producción (`FER/.github/workflows/release.yml:34,40`). Hay que confirmar qué `API_URL` y qué CSP terminan dentro del bundle productivo.

### Diagrama del flujo de autenticación

```mermaid
sequenceDiagram
    autonumber
    participant B as Navegador · Angular (admisiones.ort.edu.uy)
    participant G as Google reCAPTCHA
    participant A as API (apiadmisiones*.ort.edu.uy)
    participant L as LDAP SOAP
    participant O as Oracle (T_REFRESH_TOKEN, personas)
    participant R as Redis (2FA, flujos, rate limit)

    B->>G: execute(action) → token v3
    B->>A: OPTIONS /auth/login (preflight: Content-Type JSON + X-Captcha-Token)
    A-->>B: 204 + ACAO si Origin ∈ allowlist (igual en todos los ambientes)
    B->>A: POST /auth/login {doc, password} withCredentials
    A->>G: siteverify (score)
    A->>R: rate limit IP / IP+doc
    A->>L: bind(doc, password)
    alt score bajo / 2FA
        A->>R: sesión 2FA (GUID), código por email (person.Email)
        A-->>B: 202 {sessionId, maskedEmail}
        B->>A: POST /auth/verify-two-factor-code {sessionId, code}
    end
    A->>O: upsert 1 refresh por persona (SHA-256 del token)
    A-->>B: 200 {person} + Set-Cookie X-Access-Token (JWT HS256, 15 min) y X-Refresh-Token (512 bits, 7 días)<br/>HttpOnly, Secure, SameSite=Strict (solo Prod/Preprod) o None, Domain=Authentication:CookieDomain
    Note over B: Angular guarda solo {docType, docNumber, firstName} en un signal en memoria
    B->>A: GET /person/details (cookie automática) [authMatchGuard en cada canMatch]
    A->>A: JwtBearer.OnMessageReceived: cookie → (fallback) Authorization: Bearer
    A->>A: [Authorize] → CurrentUserService(sub) → caso de uso → filtro por CodigoPersona
    A->>O: Devart / EF → Oracle
    A-->>B: 200 JSON
    B->>A: request → 401 sin body
    B->>A: POST /auth/refresh-token (sin body; dedupe, 1 reintento, /Auth/ excluido)
    A->>O: buscar hash activo → nuevo par, ExpiresAt = ahora + 7 días (deslizante)
    A-->>B: Set-Cookie nuevos / o borrado si falla
    B->>A: POST /auth/logout (sin body)
    A->>O: revoca refresh (IsActive=NO); el access JWT sigue válido hasta exp
    A-->>B: borra las 3 cookies; Angular limpia el signal (no sessionStorage)
```

---

## 2. Inventario de cookies

El único emisor de cookies es `WA/Security/Authentication/CookieAuthenticationHelper.cs`. El front no lee ni escribe cookies: no hay `document.cookie`.

| Cookie | Contenido | HttpOnly | Secure | SameSite | Domain | Path | Duración | Borrado | Prefijo |
|---|---|---|---|---|---|---|---|---|---|
| `X-Access-Token` | JWT HS256 (`sub`, `unique_name` = CodigoPersona) | Sí `:68` | Sí `:69` | Strict si Prod/Preprod, si no None `:40-52,70` | `Authentication:CookieDomain` o host-only `:30-34,73` | `/` | `Jwt:ExpireMinutes` (default 15) `AuthController.cs:404-412` | logout, refresh fallido o ausente `:182-196` | No |
| `X-Refresh-Token` | 64 bytes aleatorios en Base64; en Oracle se guarda SHA-256 | Sí `:94` | Sí `:95` | igual | igual | `/` (se envía en **todas** las requests) | `Jwt:RefreshExpireDays` (default 7), **deslizante** | igual | No |
| `X-Password-Activation` | JWT de sesión de activación | Sí `:137` | Sí `:138` | igual | igual | `/` | `PasswordActivation:SessionMinutes` = 15 | al completar la contraseña o en el logout | No |
| `XSRF-TOKEN` / antiforgery | **No existe** | — | — | — | — | — | — | — | — |

**Observaciones**

- **C-01 (a confirmar, Media).** El comentario en `CookieAuthenticationHelper.cs:26-29` indica: "En ambientes desplegados setear `Authentication:CookieDomain=.ort.edu.uy` para que la cookie se comparta entre subdominios (admisiones y FDP)". Si producción usa ese valor:
  - El JWT y el refresh viajan a **todo servidor `*.ort.edu.uy`**, incluidos los hosts desa y testing de esta misma app.
  - Las cookies de distintos ambientes, con igual nombre, dominio y path, **se pisan entre sí** en el navegador.
  - Habilita *cookie tossing* desde cualquier subdominio.
  - Impide usar el prefijo `__Host-`.

  El valor vive en Key Vault (`README.md:126`). Además, el issuer y la audiencia están fijos a `https://admisiones.ort.edu.uy` en todos los ambientes (`AuthenticationExtensions.cs:14`). Hay que confirmar que `Jwt:SecretKey` sea distinto por ambiente; si no lo es, un token de desa vale en producción.
- **SameSite apropiado.** En **todos** los ambientes desplegados, front y API son *same-site* (`admisionesX` ↔ `apiadmisionesX`), así que `Strict` funciona en todos. `None` solo hace falta en ejecución local (`http://localhost:4200` → `https://localhost:7150` es *cross-site* por esquema). Además, el comentario de `:37-38` confunde *cross-origin* con *cross-site*. `Lax` no aporta nada sobre `Strict` en este flujo: la SPA no depende de navegaciones top-level con cookie.
- **Duplicación.** Por diseño hay un solo refresh por persona (PK `(CODIGO_PERSONA, SISTEMA)`, `DataAccess/Services/RefreshTokenService.cs:38-60`). Un login en otro dispositivo invalida el refresh anterior; el access token anterior sigue vivo hasta que expira.
- **Path del refresh.** Con `Path=/`, el refresh token viaja en cada request a la API. Con un path limitado a la ruta de `auth/`, solo viajaría a refresh y logout.

---

## 3. CSRF de extremo a extremo

### 3.1 Los cuatro eslabones

| Eslabón | Estado | Evidencia |
|---|---|---|
| 1. Emisión del token (API) | **Ausente.** No hay `AddAntiforgery`, `IAntiforgery`, cookie `XSRF-TOKEN` ni endpoint que emita token | grep `antiforgery\|xsrf\|csrf\|RequestVerificationToken` sobre `WebApiAdmisiones/`: sin resultados |
| 2. Recepción en Angular | **Ausente.** No existe cookie que leer. Aunque la API la emitiera, sería del host `apiadmisiones*` y `document.cookie` del front no la vería, salvo con `Domain=.ort.edu.uy`, lo que la expone a todos los subdominios | `FE/` grep `document.cookie\|xsrf\|csrf`: sin resultados |
| 3. Envío del header | **Inoperante.** `provideHttpClient(withXhr(), withInterceptors([...]))` deja activo el interceptor XSRF por defecto (cookie `XSRF-TOKEN`, header `X-XSRF-TOKEN`), pero ese interceptor saltea toda request de otro origen y no tiene token. No hay interceptor propio que lo reemplace | `FE/app.config.ts:54-57`; `@angular/common` `_module-chunk.mjs:1918-1944, 1837-1844` |
| 4. Validación (API) | **Ausente.** No hay filtro antiforgery ni chequeo de `Origin`/`Sec-Fetch-Site`. CORS no bloquea la *ejecución* de una simple request: solo omite los headers ACAO. `UseCors` está en `MiddlewarePipelineExtensions.cs:47` y no hay middleware que corte | grep `Headers.Origin\|Sec-Fetch`: sin resultados |

**Qué protege hoy, y hasta dónde:**

- **Preflight CORS.** Toda acción con `[FromBody]` exige `application/json`. `JsonSchemaValidationFilter` responde 415 a cualquier otro `Content-Type` cuando hay body (`WA/Security/RequestValidation/JsonSchemaValidationFilter.cs:112-132`), y el binder de `[ApiController]` rechaza un body vacío. JSON obliga a preflight, y el preflight solo pasa para la allowlist. **No cubre:**
  1. Requests **sin body**: con `Content-Length: 0` el filtro no valida `Content-Type` (`:67-79`).
  2. `multipart/form-data`, que el filtro admite (`:19-23`, por ADR-019) y que es un content-type *CORS-safelisted*.
- **SameSite=Strict.** Bloquea el envío de la cookie desde sitios ajenos, **solo en Prod/Preprod** y **no** desde `*.ort.edu.uy`.
- **Headers custom obligatorios** (`X-Captcha-Token`, `X-Flow-Id`): fuerzan preflight en los endpoints que los exigen.

### 3.2 Matriz CSRF por endpoint (31 acciones mutantes)

Referencias de las columnas:

- **Simple?** indica si un navegador puede enviar la request sin preflight.
- **Prod** = `SameSite=Strict`; **No-prod** = `None`.
- *Same-site* = cualquier `https://*.ort.edu.uy`.

| # | Método y ruta | Auth | Body / requisito | Simple? | Prod: ajeno / same-site | No-prod: cualquier sitio | Riesgo |
|---|---|---|---|---|---|---|---|
| 1 | POST `auth/login` | anon | JSON + `X-Captcha-Token` | No | — / — | — | OK (login CSRF bloqueado por header custom) |
| 2 | POST `auth/verify-two-factor-code` | anon | JSON | No | — | — | OK |
| 3 | POST `auth/resend-two-factor-code` | anon | JSON | No | — | — | OK |
| 4 | POST `auth/activate-password-link` | anon | JSON | No | — | — | OK |
| 5 | POST `auth/complete-initial-password` | cookie `X-Password-Activation` | JSON | No | — | — | OK |
| 6 | POST `auth/logout` | anon (usa cookies) | **sin body** | **Sí** | no / **sí** | **sí** | Baja: cierre de sesión forzado |
| 7 | POST `auth/refresh-token` | anon (refresh cookie) | **sin body** | **Sí** | no / **sí** | **sí** | Baja por sí sola; **habilitador**: renueva el access y extiende la ventana de V-01 a 7 días desde la última actividad |
| 8 | POST `auth/recover-password` | anon | JSON + captcha | No | — | — | OK |
| 9 | POST `registration/evaluate-document` | anon | JSON + captcha | No | — | — | OK |
| 10 | POST `registration/verify-identity` | anon | JSON + captcha + `X-Flow-Id` | No | — | — | OK |
| 11 | POST `registration/analyze-attachment` | anon | multipart + **`X-Captcha-Token`** | No (header custom) | — | — | OK (además anónimo) |
| 12 | POST `registration/confirm-new-person` | anon | JSON + captcha + `X-Flow-Id` | No | — | — | OK |
| 13 | POST `registration/confirm-registration-request` | anon | JSON + captcha + `X-Flow-Id` | No | — | — | OK |
| 14 | PUT `person/details` | JWT | JSON (incluye **email y documento**) | No | — | — | OK frente a CSRF; ver V-02 y V-03 |
| 15 | POST `person/validate-phone-number` | anon | JSON | No | — | — | OK (sin estado) |
| 16 | POST `person/change-password` | JWT | JSON + contraseña actual | No | — | — | OK |
| 17 | POST `person/photo` | JWT | **multipart** | **Sí** | no / **sí** | **sí** | **Alta**: reemplaza la foto |
| 18 | POST `person/identity-document` | JWT | **multipart** (fecha + frente + dorso) | **Sí** | no / **sí** | **sí** | **Alta**: reemplaza el documento de identidad |
| 19 | POST `enrollments/product-interest` | JWT | JSON | No | — | — | OK |
| 20 | POST `enrollments/initial-survey` | JWT | JSON | No | — | — | OK |
| 21 | POST `enrollments/confirm-pre-enrollment` | JWT | JSON | No | — | — | OK |
| 22 | POST `enrollments/reactivate` | JWT | JSON | No | — | — | OK |
| 23 | POST `enrollments/start-payment` | JWT | JSON | No | — | — | OK |
| 24 | POST `scholarships/applications` | JWT | JSON | No | — | — | OK |
| 25 | POST `scholarships/applications/accept?applicationId=` | JWT | **sin body**, id en query | **Sí** | no / **sí** | **sí** | **Alta**: acepta una beca otorgada en nombre del usuario. Los ids ajenos dan 404 sin efecto, así que se puede barrer un rango |
| 26 | PUT `scholarships/affidavit?applicationId=` | JWT | JSON | No | — | — | OK |
| 27 | POST `scholarships/affidavit/confirm?applicationId=` | JWT | JSON | No | — | — | OK |
| 28 | POST `scholarships/affidavit-document` | JWT | **multipart** (`AffidavitId` + archivo) | **Sí** | no / **sí** | **sí** | **Alta**: reemplaza el certificado o formulario de la declaración jurada |
| 29 | DELETE `scholarships/affidavit-document` | JWT | — | No (método) | — | — | OK |
| 30 | POST `scholarships/income-receipt` | JWT | **multipart** (`MonthlyIncomeId` + archivo) | **Sí** | no / **sí** | **sí** | **Alta**: reemplaza comprobantes de ingreso |
| 31 | DELETE `scholarships/income-receipt` | JWT | — | No (método) | — | — | OK |

**Escenarios del pedido que no tienen endpoint en esta API:**

- Cambio de correo como operación separada: está dentro del PUT #14.
- Modificación de permisos: no hay roles ni policies; `AddAuthorization()` va sin opciones en `WA/Program.cs:109`.
- Operaciones administrativas: no hay endpoints de backoffice.

**GET que cambie estado: ninguno.**

- Las lecturas no escriben en la base, no mandan mail ni escriben en Redis, salvo la caché de catálogos con clave fija.
- `GetAffidavit` declara explícitamente que "un GET no escribe" (`AL/AppLogic.Scholarships/Affidavit/Services/AffidavitService.cs:175-177`).
- Queda a confirmar si el GET externo `ORTSecure/Pagos/Carritos` que llama `enrollments/details` crea carritos del lado legacy.

### 3.3 Efecto de SameSite, Origin y Content-Type (sin falsos positivos)

- **Content-Type.** `text/plain` y `application/x-www-form-urlencoded` con body reciben 415 (`JsonSchemaValidationFilter.cs:121-129`). Un `<form>` HTML clásico contra un endpoint JSON **no** funciona. Lo explotable es lo que la tabla marca como *simple*: `fetch(url, {method:'POST', mode:'no-cors', credentials:'include', body: formData})` o un `<form>` vacío.
- **Accept.** El navegador manda `*/*` tanto en forms como en fetch y el filtro lo acepta (`:201-219`). Aunque terminara en 406, la negociación ocurre después de ejecutar la acción.
- **Origin.** El navegador lo envía siempre en un POST cross-origin, pero la API no lo mira.
- **SameSite.**
  - En Prod/Preprod (`Strict`) un sitio externo no puede explotar V-01.
  - En producción, una página en cualquier `https://*.ort.edu.uy` sí puede, por ejemplo con XSS en otro sistema de ORT, un subdominio abandonado (*takeover*) o contenido de usuarios publicado bajo `ort.edu.uy`.
  - En ambientes con `None`, cualquier sitio puede. Safari y Firefox (ETP), y Chrome en incógnito, bloquean cookies de terceros por defecto, lo que reduce la explotación en esos navegadores. Chrome estándar las envía.
- **Ventana de explotación.** Con #7 (refresh sin body), el atacante primero obtiene un access token nuevo (el `Set-Cookie` de una respuesta *no-cors same-site* se guarda) y después ejecuta la acción. La víctima no necesita una sesión activa: le alcanza con haber usado la app en los últimos 7 días (refresh deslizante).

---

## 4. CORS y orígenes

`WA/Extensions/ServiceCollectionExtensions.cs:176-218`, aplicado globalmente en `MiddlewarePipelineExtensions.cs:47`:

- `WithOrigins(...)`: 8 orígenes fijos e **idénticos en todos los ambientes**:
  - `http://localhost:4200`, `http://localhost:5001`
  - `https://admisiones.ort.edu.uy`
  - `https://admisionespreprod.ort.edu.uy`
  - `https://admisionestesting.ort.edu.uy`, `https://admisionestesting2.ort.edu.uy`
  - `https://admisionesdesa.ort.edu.uy`, `https://admisionesdesa2.ort.edu.uy`
- `AllowAnyHeader()`, `AllowCredentials()`, `WithExposedHeaders(Content-Disposition, Retry-After, X-RateLimit-*)`.
- No hay `AllowAnyOrigin` ni `SetIsOriginAllowed`. Un origen fuera de la lista recibe un preflight 204 sin ACAO, y el navegador bloquea.

**V-02 (Alta).** En producción, los 5 orígenes no productivos son *same-site* con la API productiva. Por lo tanto:

1. Las cookies `Strict` productivas viajan en requests originadas en esas páginas.
2. CORS, con `AllowCredentials`, **autoriza a leer la respuesta**.

Un JS ejecutándose en `admisionesdesa.ort.edu.uy` (ramas sin revisión desplegadas, un XSS en desa o un servidor de desa comprometido) puede, con la sesión productiva de quien visite esa URL:

- leer `person/details` y el documento de identidad;
- leer la declaración jurada con ingresos;
- ejecutar cualquier mutación JSON, incluida la cadena de V-03.

`http://localhost:*` es *cross-site* respecto de producción, así que las cookies `Strict` no viajan y no hay riesgo de cookie en producción. En los ambientes con `None`, cualquier proceso local que sirva en esos puertos lee datos autenticados.

**Tests existentes que no protegen nada.** `UnitTesting/Extensions/ServiceCollectionExtensionsTests.cs:219-305` declaran orígenes esperados que no coinciden con la lista real y solo hacen `Assert.NotEmpty`. Ya estaba señalado en la ficha 3.4.2.

**Necesidad de CORS.**

- Con la arquitectura actual (cross-origin), CORS es necesario, pero solo para el origen del front **del mismo ambiente**.
- Si se pasa a *same-origin*, debe eliminarse en producción.
- No se recomienda ampliar la lista ni usar `SameSite=None`.

---

## 5. XSS en Angular

| Vector | Resultado | Evidencia |
|---|---|---|
| `[innerHTML]`, `innerHTML`, `outerHTML`, `insertAdjacentHTML` | No se encontró | grep sobre `FE/` |
| `DomSanitizer`, `bypassSecurityTrust*`, `SafeHtml/Url/ResourceUrl` | No se encontró | idem |
| `Renderer2`, `document.write`, `eval`, `new Function`, `<script>` dinámico, `iframe`/`srcdoc` | No se encontró | idem |
| `ElementRef.nativeElement` | Solo `focus`/`querySelector`, sin escrituras | `two-factor-validation.ts:109,153`, `dashboard.ts:79`, etc. |
| Bindings de URL con dato del backend | Uno: `[href]="'mailto:' + coordinator.email"`. El esquema queda fijo por el literal y pasa por el sanitizer de URL | `FE/features/enrollments/.../enrollment-success-step.html:32` |
| Contenido del backend | Se muestra por interpolación `{{ }}`, que escapa | p. ej. `scholarship-card.html:7` |
| Renderizado dentro de librerías (no verificable) | `ort-icon [svgContent]="getBankSvg(bank.name)"`, snackbar con mensajes del backend, previews de `ort-file-uploader` con el MIME del servidor | `responsive-select.html:57,73,163`; `snackbar-handler.ts:46-55` |
| Librerías HTML/markdown | Ninguna en el lockfile (marked, dompurify, sanitize-html, ngx-markdown, etc.) | `FER/package-lock.json` |
| Trusted Types | No | — |

**V-06 (Baja). CSP con nonce fijo.**

- La CSP real vive en Azure (`CSP_POLICY_TEMPLATE`), inyectada en `web.config.template:16`.
- La plantilla documentada usa `script-src 'self' 'nonce-admisiones-recaptcha-2026' 'strict-dynamic' ...`, y la doc reconoce que es un "valor fijo por ambiente" porque IIS sirve archivos estáticos (`FER/docs/CONFIGURACION_AMBIENTE_AZURE.md:170-177`).
- Un nonce público y constante no protege: una inyección de HTML con `<script nonce="admisiones-recaptcha-2026">` pasa la política.
- Como no hay sinks HTML en la app, el impacto queda acotado a una inyección futura o dentro de una librería.
- **A confirmar:** los workflows de CI ejecutan `npx ng build` directo y no `npm run build`, así que no corre `inject-script-nonce` (`FER/.github/workflows/release.yml:40`, `dev-test-deploy.yml:54`). Con `'strict-dynamic'`, `'self'` se ignora y el bundle quedaría bloqueado. Que la app funcione indica que la CSP efectiva difiere de la documentada.

**Recordatorio.** HttpOnly impide leer el JWT, pero no impide usarlo. Cualquier JS en el origen del front, o en un origen de la allowlist CORS (V-02), hace requests autenticadas y lee las respuestas. Por eso un XSS en cualquiera de los 8 orígenes de la allowlist equivale a una toma de sesión, y con V-03, a una toma de cuenta.

**Dependencias.** `npm audit` no se ejecutó: requiere el registry y el token de GitHub Packages. El lockfile mezcla minors de Angular (core 22.1.0, common/router 22.2.1).

---

## 6. Guards e interceptors

### Interceptors (orden: XSRF por defecto → `httpInterceptor` → `ortApiErrorInterceptor` → `authRefreshInterceptor`)

| Interceptor | Comportamiento | Evidencia |
|---|---|---|
| XSRF de Angular | Activo pero inoperante (§3.1) | `FE/app.config.ts:54-57` |
| `httpInterceptor` | Agrega `X-Captcha-Token` cuando hay `CAPTCHA_ACTION` (si el captcha falla, la request no sale). Agrega headers de telemetría solo si el origen es el de `API_URL`, **pero si `API_URL` está vacío o no parsea los agrega a toda request** (sin datos sensibles: UUID de sesión, ruta, device) | `FE/core/interceptors/http.ts:24-42, 67`; `FE/core/.../telemetry.ts:261-276` |
| `ortApiErrorInterceptor` | Librería `@desarrolloort/ngx-utils`; muestra el mensaje del backend para 400/403/404/409/429 | `FE/app.config.ts:70-73` |
| `authRefreshInterceptor` | Ante un 401 **sin body**, con `withCredentials` y URL fuera de `/Auth/`, hace refresh compartido (`shareReplay`) y **un solo** reintento (`next(request)`, no re-entra al interceptor). Si el refresh falla: `clearSession()` y relanza el error | `FE/core/interceptors/http.ts:79-142` |

- **Bucles de renovación: no hay.** El endpoint de refresh queda excluido por la regex, hay deduplicación entre requests concurrentes y como máximo un reintento por request.
- **`withCredentials`.** No es global. Se aplica por endpoint (`requiresAuth` del generado) y explícitamente en `AuthApi` (`FE/shared/api/core/api-http-client.ts:266-274`; `FE/features/auth/api/auth.api.ts:159…357`).

### Guards y estado

- **Un solo guard, `authMatchGuard`** (`FE/core/guards/auth.ts:7-18`). Hace `GET /person/details` en cada `canMatch` y redirige a `/iniciar-sesion` si falla. Protege `inicio`, `inscripciones` y `becas` (`FE/app.routes.ts:6-19`).
- **El guard no es la barrera de seguridad:**
  - Todos los controllers tienen `[Authorize]` a nivel de clase, con `[AllowAnonymous]` solo en auth, registro, un catálogo y la validación de teléfono.
  - Cada caso de uso filtra por el `CodigoPersona` del token antes de cualquier efecto. **No se encontró IDOR**: `AcceptScholarship` en `ScholarshipService.cs:113-115`; archivos de la declaración jurada en `AffidavitFileService.cs:196-217`; reactivación y pago en `GetDetalleByKey(id, personId)`. La respuesta ante un id ajeno es 404, igual que "no existe".
- **Roles y cambios de rol en el cliente: no hay.** El front no deriva permisos (grep `role|permission|claims|jwt|decode`: nada) y el backend no tiene roles. "Cambio de rol con privilegios retenidos" no aplica.
- **Storage:**
  - No hay tokens en `localStorage`, `sessionStorage` ni IndexedDB.
  - El estado de usuario es un `signal` en memoria (`FE/features/auth/services/auth-session.ts:28-32`).
  - Persisten **después del logout** (**V-07**): `sessionStorage['inscription-resume-context']` (IDs de producto, proceso, ofertas e inscripciones, en `enrollment-resume-context.ts:5,24-32`), las claves de telemetría y `window.__ADMISIONES_TELEMETRY__`. Coincide con la ficha 14.3.1.
- **Credenciales en logs:**
  - Front: el único `console` es `console.error` en el bootstrap (`src/main.ts:6`). La telemetría registra solo pathname y status, sin bodies ni query strings. El token de activación se quita de la URL con `replaceState` antes de llamar a la API (`set-password.ts:211-223`).
  - API: `OnAuthenticationFailed` loguea tipo de excepción y path, nunca el token (`AuthenticationExtensions.cs:168-182`).

### Endpoints de sesión en el backend (V-04, Media)

| Aspecto | Estado | Evidencia |
|---|---|---|
| Rotación del refresh | Sí, en cada uso | `AL/AppLogic.Authentication/UseCases/RefreshTokens.cs:43-67` |
| Reutilización de un refresh rotado | Responde 401 y borra cookies. **No** hay detección de reuso ni revocación de familia (`REMPLACE_BY_TOKEN_ID` siempre `null`) | `DataAccess/Services/RefreshTokenService.cs:54` |
| Atomicidad | El upsert matchea `(persona, sistema)` y no el hash anterior: dos refresh concurrentes con el mismo token prosperan | `RefreshTokenService.cs:38-40` |
| Vida absoluta de la sesión | **No hay.** `ExpiresAt = UtcNow + RefreshExpireDays` en cada refresh | `AL/AppLogic.Authentication/Services/SessionTokenIssuer.cs:36-43` |
| Revalidación del estado en refresh | **No.** Solo `person == null`. No repite las reglas del login (alumno extranjero, estado LDAP) | `RefreshTokens.cs:54-60` |
| Logout | Revoca el refresh en Oracle (best-effort) y borra cookies. El access JWT sigue válido hasta `exp` (`OnTokenValidated` vacío; sin `jti` ni denylist) | `AL/.../UseCases/Logout.cs:23-53`; `AuthenticationExtensions.cs:156-159` |
| Cambio de contraseña | Exige la contraseña actual (vía LDAP). **No revoca** el refresh ni otras sesiones; no tiene rate limit | `AL/AppLogic.People/UseCases/ChangePassword.cs:18-42`; `PersonController.cs:122-127` |

---

## 7. Vulnerabilidades confirmadas

### V-01 · Alta · CSRF en acciones que no disparan preflight

- **Evidencia:**
  - Filtro de content-type: `WA/Security/RequestValidation/JsonSchemaValidationFilter.cs:19-23, 67-79`.
  - Acciones: `WA/Controllers/PersonController.cs:165-180, 190-221`; `ScholarshipsController.cs:87-92, 195-216, 255-274`; `AuthController.cs:316-328, 343-369`.
  - SameSite: `CookieAuthenticationHelper.cs:40-52`.
  - Ausencia de antiforgery y de `Origin`: §3.1.
- **Escenario (no-prod).** La víctima, con sesión en `admisionestesting`, visita `evil.example`, que ejecuta:
  ```js
  const f = new FormData();
  f.append('File', new Blob([pngBytes], {type:'image/png'}), 'x.png');
  fetch('https://apiadmisionestesting.ort.edu.uy/person/photo',
        {method:'POST', mode:'no-cors', credentials:'include', body:f});
  ```
  La request es simple, viaja sin preflight, lleva la cookie y la acción se ejecuta.
- **Escenario (prod).** El mismo código, servido desde cualquier página `https://*.ort.edu.uy` controlada por el atacante, funciona con `SameSite=Strict`.
- **Impacto.** Integridad de la evidencia de beca e identidad: reemplazar el documento de identidad, la foto, el certificado de la declaración jurada o los comprobantes de ingreso; aceptar una beca otorgada. Cierre o renovación forzada de sesión.

### V-02 · Alta · CORS productivo confía en orígenes no productivos con credenciales

- **Evidencia:** `WA/Extensions/ServiceCollectionExtensions.cs:178-188, 212`.
- **Precondición:** ejecución de JS en uno de los 5 hosts no productivos (o en un `localhost` en ambientes con `None`).
- **Impacto:** lectura de datos personales e imágenes de documento, y toda mutación JSON con la sesión productiva. Habilita V-03.

### V-03 · Alta (cadena) · Cambio de email y documento sin reautenticación

- **Evidencia:**
  - `AL/AppLogic.People/UseCases/UpdatePersonDetails.cs:51-66` (cambio de documento, solo valida unicidad).
  - `:73` (`ApplyIdentityChanges`), `:80` (`person.Email = ...`).
  - `:102-109` (única validación: `Email == EmailConfirmation`).
  - Excepción, para identidad restringida: `AL/AppLogic.People/Rules/PersonIdentityRules.cs:9-19`.
  - El front lo envía sin contraseña: `FE/features/auth/api/account.api.ts:55-66`.
  - El email es destino del 2FA (`LoginFlowService.cs:196`) y de la recuperación (`PasswordActivationService.cs:139-159`).
- **Cadena:**
  1. JS en un origen confiado (V-02) o XSS en el front.
  2. `GET person/details` para obtener documento y apellido.
  3. `PUT person/details` con el email del atacante.
  4. `POST auth/recover-password`.
  5. El link llega al atacante, que fija la contraseña con autologin; desde ahí, el 2FA también le llega a él.
- **Impacto adicional:** cambiar el documento modifica el identificador de login y puede bloquear el registro de un documento legítimo.

### V-04 · Media · Sesiones no revocables

Ver la tabla de §6. Un refresh robado, o una cuenta deshabilitada en LDAP, sigue operando indefinidamente mientras se use al menos una vez cada 7 días. Cambiar la contraseña no expulsa al atacante.

### V-05 · Media · SameSite falla abierto y es permisivo fuera de Prod/Preprod

- **Evidencia:** `CookieAuthenticationHelper.cs:40-52` lee `ASPNETCORE_ENVIRONMENT` directamente. ASP.NET Core asume `Production` cuando la variable no está, y la imagen no la define (`WA/Dockerfile`). Resultado: la app corre "productiva" (sin Swagger, mensajes genéricos) **con cookies `SameSite=None`**.
- **Además:** `Development` (desa) y `Testing` son `None` por diseño, aunque front y API son *same-site* también ahí.

### V-06 · Baja · CSP con nonce fijo y público

Ver §5.

### V-07 · Baja · El logout no limpia el estado del navegador

Ver §6. Afecta a equipos compartidos; los datos son IDs, sin PII.

---

## 8. Configuraciones inseguras o a confirmar

| ID | Tema | Evidencia | Estado |
|---|---|---|---|
| C-01 | `Authentication:CookieDomain=.ort.edu.uy` sugerido en el código; mismo issuer y audiencia en todos los ambientes | `CookieAuthenticationHelper.cs:26-34`; `AuthenticationExtensions.cs:14` | A confirmar en Key Vault (valor y separación de `Jwt:SecretKey` por ambiente) |
| C-02 | Sin prefijos `__Host-`/`__Secure-`; refresh con `Path=/` | §2 | Confirmado |
| C-03 | `/metrics` (Prometheus) sin autenticación ni restricción de host | `MiddlewarePipelineExtensions.cs:148` | A confirmar si el proxy lo bloquea |
| C-04 | 500 con `ex.Message` fuera de Prod/Preprod; `InputSanitizationException` con mensaje interno en **todos** los ambientes | `WA/Security/Middleware/ExceptionHandlingMiddleware.cs:102, 124-128`; `SanitizeAttribute.cs:65-67` | Confirmado |
| C-05 | `UseForwardedHeaders` sin `KnownProxies`/`KnownNetworks`: detrás de un proxy que no sea loopback, `RemoteIpAddress` es la IP del proxy y los rate limits por IP (login 10/15 min) se vuelven **globales** | `MiddlewarePipelineExtensions.cs:31-37` | A confirmar según la topología |
| C-06 | `appsettings.Production.json` apunta el servicio LDAP SOAP y `ApiServicioInterno` a hosts **desa**, LDAP por **HTTP** (las contraseñas viajan en claro hasta el servicio SOAP) | `WA/appsettings.Production.json:68,71` | A confirmar si Key Vault lo sobrescribe |
| C-07 | El release del front sincroniza el ambiente `desa` antes del build productivo | `FER/.github/workflows/release.yml:34,40` | A confirmar |
| C-08 | Pago externo: `form POST` a una URL del backend con chequeo de esquema (admite `http:`) y sin allowlist de host | `external-payment-submitter.ts:46-53` | Baja |
| C-09 | El JSON Schema Registry es de hecho código muerto (un solo esquema de ejemplo para `/api/datospersonales`) | `JsonSchemaValidationFilter.cs:257-259` | Informativo |

---

## 9. Correcciones propuestas (no aplicadas)

En orden de prioridad. Cada una es independiente.

### P0-1 · Validar `Origin` en métodos no seguros (cierra V-01 en todos los ambientes)

Es la corrección más chica que cubre también a los subdominios *same-site*. Va en `WA/Extensions/MiddlewarePipelineExtensions.cs`, después de `UseCors` y antes de `UseAuthentication`. La lista sale de la misma configuración que CORS (P0-2):

```csharp
private static IApplicationBuilder UseOriginCheck(this IApplicationBuilder app, string[] allowedOrigins) =>
    app.Use(async (context, next) =>
    {
        var request = context.Request;
        if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method) || HttpMethods.IsOptions(request.Method))
        {
            await next();
            return;
        }

        // Clientes no-navegador (k6, server-to-server con Bearer) no mandan ninguno de los dos.
        var origin = request.Headers.Origin.ToString();
        var fromBrowser = origin.Length > 0 || request.Headers.ContainsKey("Sec-Fetch-Site");
        if (fromBrowser && !allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            await context.Response.WriteErrorAsync(StatusCodes.Status403Forbidden, "Origen no permitido.", context.RequestAborted);
            return;
        }

        await next();
    });
```

No alcanza con `Sec-Fetch-Site`: las requests legítimas son `same-site`, igual que las del atacante.

### P0-2 · CORS por ambiente desde configuración (cierra V-02)

- `Cors:AllowedOrigins` por `appsettings.{env}.json`.
- Producción: solo `https://admisiones.ort.edu.uy`.
- `localhost` solo en `LocalHost`.
- Reemplazar los tests de §4 por tests que evalúen la política (ver T-3).
- Si se despliega *same-origin*, no registrar CORS en Prod/Preprod.

### P0-3 · `SameSite=Strict` por defecto y sin depender del nombre del ambiente (cierra V-05)

`Development` es a la vez el ambiente desplegado de desa y el de los perfiles locales (`Properties/launchSettings.json:18,28`), así que la decisión tiene que venir de configuración y fallar cerrada:

```csharp
private static SameSiteMode ResolveSameSite(HttpContext context) =>
    Enum.TryParse<SameSiteMode>(
        context.RequestServices.GetService<IConfiguration>()?["Authentication:CookieSameSite"],
        ignoreCase: true,
        out var mode)
        ? mode
        : SameSiteMode.Strict;
```

Con `"CookieSameSite": "None"` solo en `appsettings.LocalHost.json` y en la configuración local de quien levante `Development` contra `localhost:4200`.

### P1-1 · Reautenticación para cambiar email o documento (cierra V-03)

- Si `request.Email` o `request.Document*` difieren de los valores actuales, exigir `currentPassword` y validarlo por LDAP bind (ya se hace en `ChangePassword`).
- Avisar al email anterior.
- Validar el formato del email en el servidor (hoy no se valida).

### P1-2 · Token anti-CSRF completo (defensa en profundidad sobre P0-1)

Depende de la arquitectura final:

- **Same-origin** (API bajo `/api` del front, `API_URL="/api"`):
  - API: `AddAntiforgery(o => { o.HeaderName = "X-XSRF-TOKEN"; o.Cookie.Name = "__Host-X-CSRF"; o.Cookie.SameSite = SameSiteMode.Strict; })` y el filtro global `AutoValidateAntiforgeryTokenAttribute` en `AddApiControllers`.
  - Un `GET auth/xsrf` que hace `GetAndStoreTokens` y escribe `XSRF-TOKEN` (no HttpOnly, host-only).
  - Angular no cambia: el interceptor nativo ya está activo.
- **Cross-origin same-site** (actual):
  - La misma configuración de antiforgery, pero el request token se devuelve **en el body** de `GET auth/xsrf` (solo legible por orígenes CORS permitidos).
  - Un interceptor funcional de Angular lo agrega como `X-XSRF-TOKEN` **solo** a requests con el origen de `API_URL`.
  - No usar una cookie XSRF con `Domain=.ort.edu.uy`: los subdominios podrían leerla o sobrescribirla.
- **En ambos casos:** el token de ASP.NET Core queda ligado a la identidad (`sub`), así que hay que volver a pedirlo después de login, 2FA, `complete-initial-password` y logout.

### P1-3 · Sesión revocable (cierra V-04)

- `ChangePassword`: después del éxito, `RevokeRefreshTokenAsync(personId, ...)`. El método ya existe y lo usa `Logout`.
- Guardar la fecha del login original y no extender `ExpiresAt` más allá de `login + N días`.
- Repetir en el refresh las reglas de bloqueo del login.
- El access token de 15 minutos sin revocación es aceptable si se hace lo anterior. Si no lo es, agregar un `session_version` validado en `OnTokenValidated` (cuesta un acceso a Redis por request).

### P2

- **Cookies:**
  - Sin `Domain` (host-only).
  - Prefijo `__Host-` en el access token.
  - Refresh con `__Secure-` y `Path` limitado a las rutas `auth/` (C-01, C-02).
  - Si FDP necesita la sesión, resolverlo con un token propio, no compartiendo la cookie con todo `ort.edu.uy`.
- **Logout en el front:** `clearSession()` también borra `inscription-resume-context` y la telemetría (V-07).
- **CSP:**
  - Con IIS estático no hay nonce por respuesta.
  - Alternativas: hashes del loader de reCAPTCHA en lugar de nonce + `strict-dynamic`, o servir `index.html` desde un componente que genere el nonce.
  - Como mínimo, sacar el nonce de la documentación y rotarlo (V-06).
- **Configuración:** `/metrics` en otro puerto o con `RequireHost`; `ForwardedHeaders` con la red del proxy; mensajes de `InputSanitizationException` genéricos (C-03, C-04, C-05).

---

## 10. Tests automatizados recomendados

Todos locales, sin tráfico contra ambientes compartidos.

| ID | Nivel | Qué verifica | Notas |
|---|---|---|---|
| T-1 | xUnit, sin infraestructura | **Test de arquitectura:** recorre por reflexión todas las acciones `HttpPost/Put/Patch/Delete` de `WebApiAdmisiones.Controllers` y falla si alguna no tiene parámetro `[FromBody]` y no está cubierta por el filtro antiforgery u Origin | Hoy fallaría con las 7 acciones de V-01. Habría detectado la regresión de ADR-019 |
| T-2 | xUnit | `CookieAuthenticationHelper`: el `Set-Cookie` contiene `httponly`, `secure`, `samesite=strict` **también con el ambiente sin definir**, y no contiene `domain=` si no está configurado; `ClearAuthenticationCookies` emite un borrado con los mismos atributos | `DefaultHttpContext` |
| T-3 | xUnit | Política CORS evaluada con `ICorsService.EvaluatePolicy` bajo configuración `Production`: `https://admisiones.ort.edu.uy` permitido con credenciales; `admisionesdesa`, `http://localhost:4200` y `https://evil.example` **no** permitidos | Reemplaza `ServiceCollectionExtensionsTests.cs:219-305` |
| T-4 | Integración (`WebApplicationFactory`; hay que agregar `Microsoft.AspNetCore.Mvc.Testing`) | Por endpoint: sin cookie → 401; JWT mal firmado, `alg:none` o expirado (más de 30 s de skew) → 401; **POST multipart y `applications/accept` sin body con `Origin: https://evil.example` → 403**; con origen permitido y sin `X-XSRF-TOKEN` → 400, con token válido → 2xx; preflight desde un origen ajeno → sin `Access-Control-Allow-Origin`; un id de otra persona → 404 sin efecto; los bodies de `auth/*` no contienen `accessToken`/`refreshToken` | Reemplazar Redis, Oracle, LDAP e `IRateLimiterService` con `ConfigureTestServices`. Los secretos requeridos por `ValidateRequiredSecrets` se pasan por variables de entorno del proceso de test. JWT de prueba con clave de test |
| T-5 | Integración | Refresh: un token rotado → 401 y cookies borradas; un token revocado por logout → 401; después de `change-password`, el refresh previo → 401 (falla hoy); la sesión no supera la vida absoluta | idem |
| T-6 | Vitest (Angular) | Con `provideHttpClientTesting`: en la variante elegida, un POST, PUT o DELETE a `API_URL` lleva `X-XSRF-TOKEN`; ninguna request a otro origen (Google, gateway de pago) lo lleva | `HttpTestingController` |
| T-7 | Vitest | `authRefreshInterceptor`: N requests concurrentes con 401 producen 1 solo refresh; un refresh con 401 no reintenta y llama a `clearSession`; las URLs `/auth/*` nunca disparan refresh; un 401 con body no dispara refresh | Verificar si `http.spec.ts` ya lo cubre |
| T-8 | Vitest | El logout deja `sessionStorage` sin `inscription-resume-context` | Falla hoy |
| T-9 | Playwright (`@security`, ambiente local) | Después del login, `context.cookies()`: `X-Access-Token`/`X-Refresh-Token` con `httpOnly`, `secure`, `sameSite:'Strict'` y dominio sin punto inicial; `document.cookie` no los contiene | — |
| T-10 | Playwright | Página PoC servida desde otro origen local (`http://127.0.0.1:5555`) que hace `fetch no-cors` multipart a `person/photo`: debe dar 403 y la foto no debe cambiar | Prueba V-01 de extremo a extremo |

El workflow ZAP existente no detecta este CSRF: su regla "Absence of Anti-CSRF Tokens" analiza formularios HTML, no APIs JSON o multipart. La cobertura debe venir de T-1, T-4 y T-10.
