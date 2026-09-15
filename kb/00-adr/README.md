# Architecture Decision Records

Decisiones técnicas y su justificación. Use [la plantilla](template-adr.md) y el próximo ID correlativo `ADR-NNN` (el que sigue es `ADR-015`). Una decisión aceptada se cambia mediante otro ADR, no reescribiendo la historia.

## Índice

Todos los ADR listados abajo son **retrospectivos**: reconstruidos por arqueología del historial de git anterior al baseline de seguridad (ver [`kb/60-descubrimiento/ruta-adr-historicos.md`](../60-descubrimiento/ruta-adr-historicos.md)). Todos están en `status: draft` — ninguno pasó todavía por revisión de equipo.

| ADR | Título | Confianza | Dominio | Nota |
|---|---|---|---|---|
| [001](ADR-001-consolidacion-monorepo-subtree.md) | Consolidación en monorepo vía `git subtree` | alta | estructura-repositorio | Motivo real confirmado por testimonio directo |
| [002](ADR-002-extraccion-api-admisiones-de-newapi.md) | Extracción de `api-admisiones` desde plantilla `NewApi` | alta | estructura-backend | Motivo explícito en el commit |
| [003](ADR-003-integracion-servicio-inscripciones-pagos.md) | Integración con "Inscripciones y Pagos" vía JWT de servicio | alta | integraciones-servicios-internos | |
| [004](ADR-004-reconocimiento-documentos-azure.md) | Reconocimiento de documentos vía Azure Document Intelligence/Face | alta | tratamiento-datos-personales | ⚠️ Requiere revisión de legal/DPO — datos biométricos |
| [005](ADR-005-redis-cache-y-estado.md) | Adopción de Redis como caché y almacén de estado | alta | infraestructura-persistencia | |
| [006](ADR-006-modularizacion-applogic-ingles.md) | Modularización de `AppLogic` + traducción de la API a inglés | alta | estructura-backend | El mejor documentado — fuente primaria del propio equipo |
| [007](ADR-007-persistencia-devart-oracle.md) | Persistencia vía Devart sobre Oracle preexistente | **media** | persistencia | Sin validar |
| [008](ADR-008-frontend-bootstrap-angular-template.md) | Frontend bootstrapped desde `angular-template` | alta | estructura-frontend | **Aceptado** (2026-09-15, testimonio del autor) |
| [009](ADR-009-hardening-seguridad-api-publica.md) | Hardening iterativo de seguridad pública | **media** | seguridad-api | Sin validar — confirmar config vigente en producción |
| [010](ADR-010-login-tipodocumento-recuperacion-jwt.md) | Login por documento + recuperación de password por JWT | **media** | autenticacion-autorizacion | Sin validar |
| [011](ADR-011-2fa-email.md) | 2FA por email | **media** | autenticacion-autorizacion | Sin validar |
| [012](ADR-012-azure-app-configuration-frontend.md) | Azure App Configuration en frontend | alta | infraestructura-configuracion | **Aceptado** (2026-09-15, testimonio del autor) |
| [013](ADR-013-integracion-tivenos.md) | Integración con "Tivenos" | **baja** | integraciones-externas | ⚠️ No aceptar sin definición legal/funcional — qué es Tivenos no está confirmado |
| [014](ADR-014-autenticacion-ldap-jwt-combinada.md) | Autenticación LDAP + JWT combinada | **media** | autenticacion-autorizacion | Sin validar |

Los de confianza **alta** (001-006, 008, 012) tienen motivación y consecuencias respaldadas por evidencia directa (commit, documentación contemporánea o testimonio) — `008` y `012` ya fueron revisados y **aceptados** por el autor. El resto (007, 009, 010, 011, 013, 014) sigue en `medium`/`low`, redactado para facilitar la revisión en equipo, pero necesita validación antes de pasar a `accepted` — cada uno lo aclara en su propio encabezado.

