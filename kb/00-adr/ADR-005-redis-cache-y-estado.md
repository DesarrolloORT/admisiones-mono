---
status: accepted
owner: rubino-f
updated: 2026-09-17
---

# ADR-005 — Adopción de Redis como caché distribuida y almacén de estado transitorio

> **Retrospectivo.** Reconstruido a partir de commits de `api-admisiones` (junio de 2026), con diseño documentado extensamente por el propio equipo en los mensajes de commit. Confianza **alta**. Ver [`kb/60-descubrimiento/ruta-adr-historicos.md`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-011--adopción-de-redis-como-caché-distribuida-y-almacén-de-estado-rate-limiting-flujos-de-registro-pendientes-2fa) — candidato `ARCH-HIST-011`.

## Estado

`accepted` (2026-09-17). El gap de fail-open/fail-closed se cerró verificándolo en código el 2026-09-17 (ver abajo); el de durabilidad en producción sigue abierto.

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

## Comportamiento ante Redis caído (verificado en código, 2026-09-17)

`AbortOnConnectFail = false`, `ConnectTimeout`/`SyncTimeout` 5000 ms, `ExponentialRetry(5000)`, connection string por variable de entorno `RedisConnectionStringAdmisiones` — todo en `Extensions/ServiceCollectionExtensions.cs:80-149`. No hay health check de Redis ni circuit breaker; los handlers de `ConnectionFailed`/`ConnectionRestored` solo loguean.

No hay una política única: el comportamiento depende del consumidor.

| Flujo | Con Redis caído | Ubicación |
|---|---|---|
| Rate limit login (middleware por IP) | **fail-open** (sin límite) | `RedisRateLimiterService.cs:94-109` |
| Rate limit registro/recuperación (`PublicAuth`) | **fail-open** | idem, vía `ServiceCollectionExtensions.cs:356` |
| Rate limit reenvío 2FA | **fail-open** | `TwoFactorAuthService.cs:300` |
| Registro pendiente | excepción propaga → HTTP 500 | `RedisPendingPersonStore.cs` (sin `catch`) |
| Sesión 2FA | excepción propaga → HTTP 500 (`AUTH_2FA_*_99`) | `RedisTwoFactorSessionStore.cs` + `TwoFactorAuthService.cs:138,248,363` |
| Caché de imágenes de documento | degrada a cache miss | `RedisIdentityDocumentImageCache.cs:78` |

El fail-open del rate limiting es deliberado y está comentado como tal en el código, con la alternativa señalada en el propio comentario.

**Inconsistencia detectada (a resolver, no es una decisión tomada):** `RedisRateLimiterService` decide fail-**open** en `IsAllowedAsync` pero fail-**closed** en `GetRemainingAsync`, que ante cualquier excepción devuelve `0` (`:132-136`). `LoginFlowService.cs:86-110` interpreta `remaining == 0` como bloqueo, de modo que **con Redis caído todo login devuelve 429 con un mensaje de "múltiples intentos fallidos" que no corresponde a lo que pasó**. El efecto neto contradice la intención fail-open del middleware. No es una decisión de este ADR: es un comportamiento emergente a corregir.

## Gaps abiertos

- Política de disponibilidad y persistencia de Redis en producción (cluster, réplica, RDB/AOF o instancia efímera). Relevante porque Redis guarda estado de negocio: si es efímero, un reinicio pierde registros pendientes y sesiones de 2FA en curso.
- Decidir explícitamente la postura fail-open vs. fail-closed y unificarla, cerrando la inconsistencia de arriba.

## Referencias

- Candidato de descubrimiento: [`ARCH-HIST-011`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-011--adopción-de-redis-como-caché-distribuida-y-almacén-de-estado-rate-limiting-flujos-de-registro-pendientes-2fa)
- Commits fuente: `858b455fee5f237675e47df6ed6ad68cb7f1afc4`, `e3a320e629854e889450f5ab47182617133d5e08`, `ed5b86b2`, `4eb11863`, `f78213bbcf9d98b809f943293c259b4f3d38ada9`
- Relacionado: `ARCH-HIST-009` (rate limiting original, en memoria), `ARCH-HIST-012` (2FA, usa Redis para sesión)
