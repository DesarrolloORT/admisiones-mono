---
status: draft
owner: rubino-f
updated: 2026-09-15
---

# ADR-005 — Adopción de Redis como caché distribuida y almacén de estado transitorio

> **Retrospectivo.** Reconstruido a partir de commits de `api-admisiones` (junio de 2026), con diseño documentado extensamente por el propio equipo en los mensajes de commit. Confianza **alta**. Ver [`kb/60-descubrimiento/ruta-adr-historicos.md`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-011--adopción-de-redis-como-caché-distribuida-y-almacén-de-estado-rate-limiting-flujos-de-registro-pendientes-2fa) — candidato `ARCH-HIST-011`.

## Estado

`draft` — pendiente de revisión y validación por el equipo. No marcar `accepted` sin esa revisión explícita.

## Contexto

`api-admisiones` corre en múltiples instancias (ver rate limiting previo en memoria, `ARCH-HIST-009`), lo cual no permite rate limiting ni caché consistentes entre instancias. Además, el flujo de registro y el nuevo 2FA por email necesitan almacenar estado transitorio (registro pendiente, sesión de 2FA) en algún lugar accesible entre requests.

## Problema

¿Cómo lograr rate limiting distribuido, caché de catálogos y almacenamiento de estado transitorio de flujos, de forma consistente entre instancias de la API?

## Opciones consideradas

No hay evidencia de alternativas evaluadas (rate limiting en memoria por instancia, otro store como Memcached, o persistencia de estado transitorio en la base de datos SQL existente).

## Decisión

Adoptar **Redis** como pieza de infraestructura compartida para tres usos:

1. `858b455f` (2026-06-02): rate limiting distribuido (login, sliding window, alineado a OWASP, HTTP 429) y caché de catálogos de solo lectura (TTL 24h).
2. `e3a320e6` (2026-06-04): índice de registros pendientes por documento, para evitar flujos de registro duplicados — Redis pasa a guardar **estado de negocio**, no solo caché.
3. `f78213bb` (2026-06-03) / `ed5b86b2` (2026-06-05): sesión de 2FA por email y caché de imágenes de documentos de onboarding.

## Consecuencias

- Redis se vuelve una dependencia de infraestructura crítica: login (rate limiting), catálogos (caché) y registro/2FA (estado transitorio) dependen de su disponibilidad.
- Al usarse también para estado de negocio (no solo caché efímera), corresponde definir una política de durabilidad/backup — no confirmada en el código revisado.

## Gaps

- Confirmar política de disponibilidad y persistencia de Redis en producción (cluster, RDB/AOF, o instancia efímera).
- Confirmar comportamiento de fail-open vs fail-closed si Redis no responde, tanto para rate limiting como para flujos de registro/2FA.

## Referencias

- Candidato de descubrimiento: [`ARCH-HIST-011`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-011--adopción-de-redis-como-caché-distribuida-y-almacén-de-estado-rate-limiting-flujos-de-registro-pendientes-2fa)
- Commits fuente: `858b455fee5f237675e47df6ed6ad68cb7f1afc4`, `e3a320e629854e889450f5ab47182617133d5e08`, `ed5b86b2`, `4eb11863`, `f78213bbcf9d98b809f943293c259b4f3d38ada9`
- Relacionado: `ARCH-HIST-009` (rate limiting original, en memoria), `ARCH-HIST-012` (2FA, usa Redis para sesión)
