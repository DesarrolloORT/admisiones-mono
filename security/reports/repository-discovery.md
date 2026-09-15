# Repository discovery

Fecha: 2026-09-15. Commit inspeccionado: `4bb986030f9efe8b0e24d33a79366d0593e97716`.

## Componentes

- Frontend: `admisiones/`, Angular 22.0.x, TypeScript 6.0.x, Vitest y Playwright. La configuración de ambiente se genera mediante `scripts/environment.js` y Azure App Configuration.
- Backend: `api-admisiones/WebApiAdmisiones/`, solución ASP.NET Core con API y proyectos de dominio/datos, todos con `net10.0`; pruebas xUnit en `UnitTesting/`.
- Despliegue: Dockerfile Linux para la API e IIS `web.config.template` para el frontend. No se encontró infraestructura como código en el root.

## Relaciones e integraciones

El frontend consume la API y servicios externos configurados por ambiente. La API integra base de datos, LDAP, correo, Redis, Tivenos y una API de inscripciones/pagos. Las URLs y credenciales requieren validación por ambiente; no se infieren valores productivos.

## Autenticación y autorización

- JWT Bearer validado en backend (issuer, audience, lifetime, firma HS256 y algoritmo permitido); token preferido desde cookie HttpOnly y fallback Bearer.
- Controllers protegidos con `[Authorize]`; endpoints públicos usan `[AllowAnonymous]`. La separación por recurso requiere pruebas específicas, no queda demostrada por el atributo.
- reCAPTCHA y rate limiting protegen flujos públicos; hay pruebas en `UnitTesting/Security/`.

## Configuración de seguridad

- CSP del frontend se materializa desde una plantilla por ambiente; desarrollo deja CSP vacía deliberadamente.
- La API agrega HSTS fuera de Development, HTTPS redirect, CORS allowlist, headers de seguridad, manejo global de excepciones y secretos requeridos al arranque.
- Existen `appsettings` diferenciados para LocalHost, Development, Testing, Preproduction y Production. No se considera que una revisión de un ambiente demuestre otro.

## CI, scanners y tests

- El frontend define lint, unit tests, cobertura, Playwright, build y Sonar en `package.json`; Sonar se configura con `sonar-project.properties`.
- No se encontró configuración ZAP versionada ni definición de pipeline en este checkout. El baseline aporta un importador, no ejecuta escaneos.
- La API cuenta con xUnit, incluidas pruebas de autenticación, captcha, controladores, CORS y middleware.

## Riesgos e incógnitas

- No hay evidencia versionada de la configuración efectiva de Production/Preproduction en Azure.
- No hay pruebas end-to-end de IDOR/BOLA identificadas en el discovery inicial.
- El fallback Bearer amplía las vías de autenticación y debe conservar cobertura equivalente a cookies.
- La protección y retención de logs y la gestión real de secretos requieren evidencia de plataforma.
- No se encontró pipeline ni baseline ZAP en el checkout; su operación y target autorizado están pendientes.

