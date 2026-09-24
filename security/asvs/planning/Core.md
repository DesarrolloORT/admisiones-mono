# Core — preparación OWASP ASVS 5.0.0

Fecha: 2026-09-16. Rama local: fix/owasp. Estado: planificación; sin cambios de código ni fixes.

Base del clone original: 7f4e060afadea40fe8c41e8a796fa6a5a1bf723f. Los cambios locales sin commit no se incluyen en el worktree de esta rama.

Responsabilidad candidata: librería backend compartida; utilidades, validadores, criptografía, conexiones DB, LDAP, correo y Azure. Confirmar sus consumidores y usos antes de atribuirle un requisito.

Fuente: [ASVS 5.0.0 oficial](https://github.com/OWASP/ASVS/tree/v5.0.0/5.0/docs_en), JSON local del monorepo con SHA-256 BCDBEC214D70ABCFAD9284A31D4F9E5134305831D628AAD3AA85D7E26626CB35. Objetivo: L2, acumulando L1 y L2; L3 queda registrado fuera de objetivo.

Este reparto es preliminar por capítulo: contiene candidatos, incluidos controles que pueden corresponder a otro componente. Todos están PENDING hasta confirmar aplicabilidad y reunir evidencia. Las descripciones exactas están en security/asvs/planning/catalogo.md del monorepo y en la fuente oficial.

## Orden propuesto de revisión

- Primero: inventariar consumidores y versión consumida; la API del monorepo fija 01239cdf6054dc5a450dcdaa3a867ae3204b61e7, diferente de la base del clone Core. No extrapolar evidencia entre esas refs.
- Luego: V1/V5/V6/V11 (validadores compartidos, inyección LDAP/SQL, política de contraseña y criptografía/hashing).
- Finalmente: V12/V13/V14/V15/V16 (TLS de integraciones, tratamiento de secretos, datos, dependencias y errores/logs).
- Coordinar API: autenticación, sesiones y autorización de la aplicación se cierran en el consumidor. Cualquier futura remediación compartida debe probarse contra las APIs consumidoras.

## Requisitos candidatos por sección

La lista completa por sección evita perder IDs durante el reparto. Ninguna sección condicional se considera NOT_APPLICABLE sin justificarlo; OAuth/OIDC, GraphQL, WebSocket y WebRTC requieren inventario previo.

### V1 — Encoding and Sanitization

Revisar codificación y sinks de salida en cliente; validación, consultas, LDAP y deserialización en servidor y librerías.

| Sección | IDs L1/L2 (PENDING) | IDs L3 (fuera del objetivo; PENDING) |
| --- | --- | --- |
| V1.1 — Encoding and Sanitization Architecture | v5.0.0-1.1.1, v5.0.0-1.1.2 | — |
| V1.2 — Injection Prevention | v5.0.0-1.2.1, v5.0.0-1.2.2, v5.0.0-1.2.3, v5.0.0-1.2.4, v5.0.0-1.2.5, v5.0.0-1.2.6, v5.0.0-1.2.7, v5.0.0-1.2.8, v5.0.0-1.2.9 | v5.0.0-1.2.10 |
| V1.3 — Sanitization | v5.0.0-1.3.1, v5.0.0-1.3.2, v5.0.0-1.3.3, v5.0.0-1.3.4, v5.0.0-1.3.5, v5.0.0-1.3.6, v5.0.0-1.3.7, v5.0.0-1.3.8, v5.0.0-1.3.9, v5.0.0-1.3.10, v5.0.0-1.3.11 | v5.0.0-1.3.12 |
| V1.4 — Memory, String, and Unmanaged Code | v5.0.0-1.4.1, v5.0.0-1.4.2, v5.0.0-1.4.3 | — |
| V1.5 — Safe Deserialization | v5.0.0-1.5.1, v5.0.0-1.5.2 | v5.0.0-1.5.3 |

### V5 — File Handling

Revisar subida, tipo real, tamaño, nombres, almacenamiento y descarga; el cliente solo aporta UX. Confirmar qué validadores pertenecen a Core.

| Sección | IDs L1/L2 (PENDING) | IDs L3 (fuera del objetivo; PENDING) |
| --- | --- | --- |
| V5.1 — File Handling Documentation | v5.0.0-5.1.1 | — |
| V5.2 — File Upload and Content | v5.0.0-5.2.1, v5.0.0-5.2.2, v5.0.0-5.2.3 | v5.0.0-5.2.4, v5.0.0-5.2.5, v5.0.0-5.2.6 |
| V5.3 — File Storage | v5.0.0-5.3.1, v5.0.0-5.3.2 | v5.0.0-5.3.3 |
| V5.4 — File Download | v5.0.0-5.4.1, v5.0.0-5.4.2, v5.0.0-5.4.3 | — |

### V6 — Authentication

Revisar LDAP, contraseña, recuperación, activación, MFA y reCAPTCHA. La UI no demuestra protección del servidor.

| Sección | IDs L1/L2 (PENDING) | IDs L3 (fuera del objetivo; PENDING) |
| --- | --- | --- |
| V6.1 — Authentication Documentation | v5.0.0-6.1.1, v5.0.0-6.1.2, v5.0.0-6.1.3 | — |
| V6.2 — Password Security | v5.0.0-6.2.1, v5.0.0-6.2.2, v5.0.0-6.2.3, v5.0.0-6.2.4, v5.0.0-6.2.5, v5.0.0-6.2.6, v5.0.0-6.2.7, v5.0.0-6.2.8, v5.0.0-6.2.9, v5.0.0-6.2.10, v5.0.0-6.2.11, v5.0.0-6.2.12 | — |
| V6.3 — General Authentication Security | v5.0.0-6.3.1, v5.0.0-6.3.2, v5.0.0-6.3.3, v5.0.0-6.3.4 | v5.0.0-6.3.5, v5.0.0-6.3.6, v5.0.0-6.3.7, v5.0.0-6.3.8 |
| V6.4 — Authentication Factor Lifecycle and Recovery | v5.0.0-6.4.1, v5.0.0-6.4.2, v5.0.0-6.4.3, v5.0.0-6.4.4 | v5.0.0-6.4.5, v5.0.0-6.4.6 |
| V6.5 — General Multi-factor authentication requirements | v5.0.0-6.5.1, v5.0.0-6.5.2, v5.0.0-6.5.3, v5.0.0-6.5.4, v5.0.0-6.5.5 | v5.0.0-6.5.6, v5.0.0-6.5.7, v5.0.0-6.5.8 |
| V6.6 — Out-of-Band authentication mechanisms | v5.0.0-6.6.1, v5.0.0-6.6.2, v5.0.0-6.6.3 | v5.0.0-6.6.4 |
| V6.7 — Cryptographic authentication mechanism | — | v5.0.0-6.7.1, v5.0.0-6.7.2 |
| V6.8 — Authentication with an Identity Provider | v5.0.0-6.8.1, v5.0.0-6.8.2, v5.0.0-6.8.3, v5.0.0-6.8.4 | — |

### V11 — Cryptography

Inventariar criptografía y hashing de Core y API; identificar algoritmo, claves, aleatoriedad y uso real antes de evaluar.

| Sección | IDs L1/L2 (PENDING) | IDs L3 (fuera del objetivo; PENDING) |
| --- | --- | --- |
| V11.1 — Cryptographic Inventory and Documentation | v5.0.0-11.1.1, v5.0.0-11.1.2 | v5.0.0-11.1.3, v5.0.0-11.1.4 |
| V11.2 — Secure Cryptography Implementation | v5.0.0-11.2.1, v5.0.0-11.2.2, v5.0.0-11.2.3 | v5.0.0-11.2.4, v5.0.0-11.2.5 |
| V11.3 — Encryption Algorithms | v5.0.0-11.3.1, v5.0.0-11.3.2, v5.0.0-11.3.3 | v5.0.0-11.3.4, v5.0.0-11.3.5 |
| V11.4 — Hashing and Hash-based Functions | v5.0.0-11.4.1, v5.0.0-11.4.2, v5.0.0-11.4.3, v5.0.0-11.4.4 | — |
| V11.5 — Random Values | v5.0.0-11.5.1 | v5.0.0-11.5.2 |
| V11.6 — Public Key Cryptography | v5.0.0-11.6.1 | v5.0.0-11.6.2 |
| V11.7 — In-Use Data Cryptography | — | v5.0.0-11.7.1, v5.0.0-11.7.2 |

### V12 — Secure Communication

Revisar TLS del hosting y de conexiones salientes HTTP, LDAP, DB, Redis y correo, por ambiente.

| Sección | IDs L1/L2 (PENDING) | IDs L3 (fuera del objetivo; PENDING) |
| --- | --- | --- |
| V12.1 — General TLS Security Guidance | v5.0.0-12.1.1, v5.0.0-12.1.2, v5.0.0-12.1.3 | v5.0.0-12.1.4, v5.0.0-12.1.5 |
| V12.2 — HTTPS Communication with External Facing Services | v5.0.0-12.2.1, v5.0.0-12.2.2 | — |
| V12.3 — General Service to Service Communication Security | v5.0.0-12.3.1, v5.0.0-12.3.2, v5.0.0-12.3.3, v5.0.0-12.3.4 | v5.0.0-12.3.5 |

### V13 — Configuration

Revisar secretos, configuración por ambiente, debug y exposición de artefactos; requieren evidencia de plataforma.

| Sección | IDs L1/L2 (PENDING) | IDs L3 (fuera del objetivo; PENDING) |
| --- | --- | --- |
| V13.1 — Configuration Documentation | v5.0.0-13.1.1 | v5.0.0-13.1.2, v5.0.0-13.1.3, v5.0.0-13.1.4 |
| V13.2 — Backend Communication Configuration | v5.0.0-13.2.1, v5.0.0-13.2.2, v5.0.0-13.2.3, v5.0.0-13.2.4, v5.0.0-13.2.5 | v5.0.0-13.2.6 |
| V13.3 — Secret Management | v5.0.0-13.3.1, v5.0.0-13.3.2 | v5.0.0-13.3.3, v5.0.0-13.3.4 |
| V13.4 — Unintended Information Leakage | v5.0.0-13.4.1, v5.0.0-13.4.2, v5.0.0-13.4.3, v5.0.0-13.4.4, v5.0.0-13.4.5 | v5.0.0-13.4.6, v5.0.0-13.4.7 |

### V14 — Data Protection

Inventariar PII, documentos, pagos, caches, almacenamiento del navegador, retención y permisos.

| Sección | IDs L1/L2 (PENDING) | IDs L3 (fuera del objetivo; PENDING) |
| --- | --- | --- |
| V14.1 — Data Protection Documentation | v5.0.0-14.1.1, v5.0.0-14.1.2 | — |
| V14.2 — General Data Protection | v5.0.0-14.2.1, v5.0.0-14.2.2, v5.0.0-14.2.3, v5.0.0-14.2.4 | v5.0.0-14.2.5, v5.0.0-14.2.6, v5.0.0-14.2.7, v5.0.0-14.2.8 |
| V14.3 — Client-side Data Protection | v5.0.0-14.3.1, v5.0.0-14.3.2, v5.0.0-14.3.3 | — |

### V15 — Secure Coding and Architecture

Revisar boundaries, dependencias, librerías compartidas, código generado y concurrencia; cruzar la versión Core consumida.

| Sección | IDs L1/L2 (PENDING) | IDs L3 (fuera del objetivo; PENDING) |
| --- | --- | --- |
| V15.1 — Secure Coding and Architecture Documentation | v5.0.0-15.1.1, v5.0.0-15.1.2, v5.0.0-15.1.3 | v5.0.0-15.1.4, v5.0.0-15.1.5 |
| V15.2 — Security Architecture and Dependencies | v5.0.0-15.2.1, v5.0.0-15.2.2, v5.0.0-15.2.3 | v5.0.0-15.2.4, v5.0.0-15.2.5 |
| V15.3 — Defensive Coding | v5.0.0-15.3.1, v5.0.0-15.3.2, v5.0.0-15.3.3, v5.0.0-15.3.4, v5.0.0-15.3.5, v5.0.0-15.3.6, v5.0.0-15.3.7 | — |
| V15.4 — Safe Concurrency | — | v5.0.0-15.4.1, v5.0.0-15.4.2, v5.0.0-15.4.3, v5.0.0-15.4.4 |

### V16 — Security Logging and Error Handling

Revisar errores públicos y redacción de telemetría y logs; protección, acceso y retención requieren evidencia operativa.

| Sección | IDs L1/L2 (PENDING) | IDs L3 (fuera del objetivo; PENDING) |
| --- | --- | --- |
| V16.1 — Security Logging Documentation | v5.0.0-16.1.1 | — |
| V16.2 — General Logging | v5.0.0-16.2.1, v5.0.0-16.2.2, v5.0.0-16.2.3, v5.0.0-16.2.4, v5.0.0-16.2.5 | — |
| V16.3 — Security Events | v5.0.0-16.3.1, v5.0.0-16.3.2, v5.0.0-16.3.3, v5.0.0-16.3.4 | — |
| V16.4 — Log Protection | v5.0.0-16.4.1, v5.0.0-16.4.2, v5.0.0-16.4.3 | — |
| V16.5 — Error Handling | v5.0.0-16.5.1, v5.0.0-16.5.2, v5.0.0-16.5.3 | v5.0.0-16.5.4 |

## Evidencia a completar por requisito

- ID oficial, aplicabilidad justificada y responsable confirmado.
- Commit y componente exactos; versión Core consumida cuando corresponda.
- Ambiente y configuración efectiva, sin incluir secretos ni PII.
- Método reproducible, resultado, fecha y enlace a evidencia/test/configuración.
- Estado sustentado por evidencia y, si existe un hallazgo, impacto y propuesta documental para una futura remediación.

No se ejecutaron builds, tests de aplicación ni escaneos en esta etapa. La validación de entrega cubre IDs, niveles, cobertura documental y diff Markdown. No se modifican contratos HTTP, configuración, dependencias, lógica ni punteros de submódulos.

Esta planificación no amplía el piloto automatizado del monorepo. Las revisiones OWASP Top 10 existentes conservan su alcance y estados; no se importan como PASS de ASVS.

Copia documental para coordinación del monorepo. La base del subtree puede diferir del clone original; la evidencia futura debe fijar el commit del componente evaluado.
