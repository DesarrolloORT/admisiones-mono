---
status: accepted
owner: rubino-f
updated: 2026-09-17
---

# ADR-003 — Integración service-to-service con la API interna "Inscripciones y Pagos" vía JWT de servicio

> **Retrospectivo.** Reconstruido a partir de commits de `api-admisiones` (abril de 2026) y de `EJEMPLO_INTEGRACION_API_INTERNA.md`, documentación contemporánea al cambio dentro del propio repo. Confianza **alta**: diseño y motivación están explícitos en los mensajes de commit y en esa documentación, no son inferencia. Ver [`kb/60-descubrimiento/ruta-adr-historicos.md`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-007--integración-service-to-service-con-la-api-interna-inscripciones-y-pagos-vía-jwt-de-servicio) — candidato `ARCH-HIST-007`.

## Estado

`accepted` (2026-09-17). Gaps funcional y de base de datos cerrados con confirmación del equipo el 2026-09-17.

## Contexto

`api-admisiones` necesita ofrecer y consultar ofertas de inscripción y datos de pagos que viven en otro sistema interno de ORT ("Inscripciones y Pagos"), separado de este monorepo. Hasta este punto, el backend solo tenía autenticación de usuario (LDAP + JWT, `ADR` pendiente de `ARCH-HIST-004`); no existía un mecanismo para que el propio backend llamara a otra API interna en nombre propio (no de un usuario).

## Problema

¿Cómo autenticar y llamar de forma segura, desde `api-admisiones`, a una API interna de terceros (Inscripciones y Pagos), sin reutilizar el JWT de usuario y dejando el patrón documentado para integraciones futuras similares?

## Opciones consideradas

No hay evidencia de alternativas formalmente evaluadas (p. ej. mTLS, API keys estáticas, mensajería asíncrona). El propio commit documenta el diseño elegido como si fuera la primera integración interna de este tipo en el repo, sentando el patrón para las siguientes.

## Decisión

1. `e802a607` (2026-04-22): agrega `InscripcionesApiClient` con métodos **atómicos y sin reintentos**, manejo de errores vía `OperationResult`, y `TokenServiceInternalApi` que genera **JWT de servicio a servicio** (distinto del JWT de usuario). Un `ServiceAuthenticationHandler` inyecta token de usuario + token de servicio + headers de trace en cada request saliente. Validación delegada a la API destino. `HttpClient` con timeout de 30s.
2. `d8f88162` (2026-04-27): conecta endpoints reales bajo `ORTSecure/...`, agrega `OfertasInscripcionService`, un controller de ejemplo y `EJEMPLO_INTEGRACION_API_INTERNA.md` documentando el flujo completo para futuras integraciones internas.

## Consecuencias

- Pagos e inscripciones dependen de la disponibilidad síncrona de una API externa a este repo, sin reintentos automáticos: una falla de esa API es visible directamente al usuario final.
- Nuevo secreto de configuración (`SECRET_KEY_API_INSCR_PAGOS`) que firma/valida el JWT de servicio — a incluir en el inventario de secretos del baseline de seguridad.
- Se documenta un patrón reusable (`EJEMPLO_INTEGRACION_API_INTERNA.md`) para integraciones internas futuras.
- **Confirmado (2026-09-17): "Inscripciones y Pagos" comparte la misma base Oracle que `api-admisiones`.** La separación es a nivel de aplicación (otra API, otro repo), no de datos. Consecuencia a tener presente: el JWT de servicio autentica la llamada HTTP, pero no impide que ambos sistemas toquen las mismas tablas por fuera de ese contrato — se suma al gap de tablas compartidas sin contrato formal de `ADR-007`/`ADR-006` (`FDP`, `LogicaORT`).
- **Confirmado (2026-09-17): el dueño funcional es el mismo equipo de Admisiones**, en otro repositorio. Las reglas de negocio de pago (pasarela, moneda, reversibilidad de transacciones) siguen sin estar documentadas en la KB, pero son incorporables sin depender de un tercero.

## Referencias

- Candidato de descubrimiento: [`ARCH-HIST-007`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-007--integración-service-to-service-con-la-api-interna-inscripciones-y-pagos-vía-jwt-de-servicio)
- Documentación contemporánea en el repo: `WebApiAdmisiones/.../EJEMPLO_INTEGRACION_API_INTERNA.md` (commit `d8f88162`)
- Commits fuente: `e802a607db1e4be31f46d1074f568de0f853c95b`, `25309f8e`, `8853eee8`, `d8f8816207d4c9354cf7a7965b884a4651c3f207`
- Relacionado: `ARCH-HIST-004` (autenticación de usuario LDAP+JWT) — este ADR introduce un JWT **de servicio**, distinto y complementario.
