---
status: accepted
owner: gabriele
updated: 2026-11-19
---

# ADR-043 — Cerrado por defecto, errores sin datos del servidor y arranque que falla con configuración insegura

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entradas del 2026-10-22 08:56 (errores sin info del servidor), 2026-11-13 08:43 (SEG-L1-03, SEG-L1-04, SEG-L1-05, SEG-L1-08), 2026-11-18 09:34 (PT-L3), 2026-11-18 10:08 (415), 2026-11-18 12:01 y 2026-11-19 08:24 (checklist de producción, PT-L4).

## Estado

`accepted` (2026-11-13). Complementa [ADR-023](ADR-023-endpoints-anonimos-defaults-seguros.md) sin modificarlo.

## Contexto

[ADR-023](ADR-023-endpoints-anonimos-defaults-seguros.md) puso `[Authorize]` de clase en cada controller, pero un controller nuevo sin el atributo quedaba anónimo. Los `appsettings` de Production y Preproduction apuntan todavía a servicios de desa por `http`. El ambiente de producción no existe aún.

## Decisión

- **`FallbackPolicy = RequireAuthenticatedUser`:** un endpoint sin atributo exige sesión. `/metrics` y `/contracts` llevan `.AllowAnonymous()` explícito. Un test de integración congela la lista de endpoints anónimos (17).
- **Validación al arrancar (`ValidateProductionEndpoints`):** en Production y Preproduction, `LdapSettings:RutaServicioLDAP` y `ApiServicioInterno:RutaApi` tienen que ser `https`, y su host no puede contener `desa` ni `testing`. Si no se cumple, la API no levanta.
- **Errores sin datos del servidor.** La API corre en Kestrel, sin IIS, así que no se copian las páginas HTML de WebApiFuncionarios:
  - un 500 responde `{ "message": "Error inesperado" }` y el detalle va al log con el correlationId;
  - un 401 sale sin body;
  - un 400/413/415 sale con `{ message }`;
  - no se envía el header `Server`.
- **Headers:** `Cache-Control: no-store` y `Cross-Origin-Resource-Policy: same-site` en `UseSecurityHeaders`. Un endpoint que necesite cache puede pisarlo.
- **Labels de Prometheus:** se aceptan hasta 100 valores distintos por label; el resto va como `other`.
- **Checklist para cuando se cree producción** (en [`PENTEST-INTERNO-2026-11.md`](../../api-admisiones/docs/PENTEST-INTERNO-2026-11.md)): relevar proxies ([ADR-035](ADR-035-ip-cliente-proxies-confianza.md)), dominio y `SameSite` de las cookies ([ADR-031](ADR-031-sesion-cookie-csrf-cors-samesite.md)), `/metrics` solo interno y Swagger apagado.

## Consecuencias

- **⚠️ Despliegue:** sin variables que pisen esas URLs, Production y Preproduction no levantan. Infra tiene que confirmar las URLs correctas.
- Una ruta inexistente pedida sin sesión responde 401 y no 404.
- Testing sigue devolviendo `ex.Message` en los 500, para depurar (decisión del usuario).
- `/metrics` está solo en la red interna en los ambientes actuales (confirmado por infra, PT-L4).

## Referencias

- [ADR-009](ADR-009-hardening-seguridad-api-publica.md): hardening de la API pública.
- [ADR-017](ADR-017-result-t-formato-unico-error.md): formato de error.
