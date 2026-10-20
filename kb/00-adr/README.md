# Architecture Decision Records

Decisiones técnicas y su justificación. Use [la plantilla](template-adr.md) y el próximo ID correlativo `ADR-NNN` (el que sigue es `ADR-031`). Una decisión aceptada se cambia mediante otro ADR, no reescribiendo la historia.

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

## Decisiones posteriores al baseline

No son retrospectivos: salen de [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), donde cada decisión quedó registrada con fecha y con quién la tomó.

| ADR | Título | Dominio |
|---|---|---|
| [015](ADR-015-secretos-iconfiguration-key-vault.md) | Secretos por `IConfiguration` y Azure Key Vault | infraestructura-configuracion |
| [016](ADR-016-mensajes-usuario-resx.md) | Mensajes al usuario en `.resx` | estructura-backend |
| [017](ADR-017-result-t-formato-unico-error.md) | `Result<T>` y un único formato de error HTTP | contratos-api |
| [018](ADR-018-mails-service-bus-contratos-tipados.md) | Mails por Service Bus con contratos tipados | integraciones-servicios-internos |
| [019](ADR-019-uploads-multipart.md) | Subida de archivos por `multipart/form-data` | contratos-api |
| [020](ADR-020-core-a-packages-ort.md) | Infraestructura de `Core` a packages `ORT.*` | estructura-backend |
| [021](ADR-021-becas-paridad-escrituras-legacy.md) | Becas: escrituras iguales al legacy | integraciones-legacy |
| [022](ADR-022-imagenes-documento-redis-cifradas.md) | Imágenes de documento cifradas en Redis | tratamiento-datos-personales |
| [023](ADR-023-endpoints-anonimos-defaults-seguros.md) | Endpoints anónimos: defaults seguros y rate limit | seguridad-api |
| [024](ADR-024-mis-becas-estado-desde-vista.md) | "Mis becas": estado desde la vista | contratos-api |
| [025](ADR-025-becas-inscripcion-obligatoria.md) | Becas: para postular hay que estar inscripto | dominio-becas |
| [026](ADR-026-becas-dj-por-fondo.md) | Becas: la DJ pregunta según el fondo; lo no preguntado queda NULL | dominio-becas |
| [027](ADR-027-becas-pdf-dj.md) | Becas: PDF de la DJ con la plantilla del legacy | integraciones-legacy |
| [028](ADR-028-becas-flujo-postulacion.md) | Becas: alta por el legacy, DJ/resumen/aceptación por la API | integraciones-legacy |
| [029](ADR-029-registro-datos-no-coinciden-solicitud-alta.md) | Registro: datos que no coinciden con el padrón → solicitud de alta | autenticacion-autorizacion |
| [030](ADR-030-link-backoffice-persona-de-la-solicitud.md) | Link del backoffice: `codigoPersona` igual al de la solicitud | seguridad-api |

