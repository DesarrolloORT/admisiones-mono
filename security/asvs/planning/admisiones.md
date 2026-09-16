# admisiones — preparación OWASP ASVS 5.0.0

Fecha: 2026-09-16. Rama local: fix/owasp. Estado: planificación; sin cambios de código ni fixes.

Base del clone original: 3e250512347e055a6684664e72b2f9a371bbba0a. Los cambios locales sin commit no se incluyen en el worktree de esta rama.

Responsabilidad candidata: frontend Angular, consumo de API, UX de autenticación y archivos, almacenamiento y telemetría del navegador, dependencias y entrega del frontend.

Fuente: [ASVS 5.0.0 oficial](https://github.com/OWASP/ASVS/tree/v5.0.0/5.0/docs_en), JSON local del monorepo con SHA-256 BCDBEC214D70ABCFAD9284A31D4F9E5134305831D628AAD3AA85D7E26626CB35. Objetivo: L2, acumulando L1 y L2; L3 queda registrado fuera de objetivo.

Este reparto es preliminar por capítulo: contiene candidatos, incluidos controles que pueden corresponder a otro componente. Todos están PENDING hasta confirmar aplicabilidad y reunir evidencia. Las descripciones exactas están en security/asvs/planning/catalogo.md del monorepo y en la fuente oficial.

## Orden propuesto de revisión

- Primero: V3 y V14.3 (cookies vistas por el navegador, CSP/headers del sitio servido, orígenes, recursos externos y datos del cliente).
- Luego: V1 y V2 (sinks de salida, URLs, validaciones UX y abuso de flujos), junto con V6/V7/V8 para contratos de autenticación, renovación y navegación.
- Finalmente: V5, V12/V13, V15/V16 (subida de archivos desde UI, hosting, builds, dependencias y telemetría).
- Coordinar API: cookies, CORS, CSRF, rate limiting, validación de archivos y autorización se prueban en servidor. Un guard o un validador Angular no los demuestra.

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

### V2 — Validation and Business Logic

Contrastar validaciones de UX con reglas confiables del servidor; revisar transiciones, concurrencia y anti-automatización.

| Sección | IDs L1/L2 (PENDING) | IDs L3 (fuera del objetivo; PENDING) |
| --- | --- | --- |
| V2.1 — Validation and Business Logic Documentation | v5.0.0-2.1.1, v5.0.0-2.1.2, v5.0.0-2.1.3 | — |
| V2.2 — Input Validation | v5.0.0-2.2.1, v5.0.0-2.2.2, v5.0.0-2.2.3 | — |
| V2.3 — Business Logic Security | v5.0.0-2.3.1, v5.0.0-2.3.2, v5.0.0-2.3.3, v5.0.0-2.3.4 | v5.0.0-2.3.5 |
| V2.4 — Anti-automation | v5.0.0-2.4.1 | v5.0.0-2.4.2 |

### V3 — Web Frontend Security

Revisar la aplicación servida y sus headers efectivos; la API emite cookies y configura CORS. Infraestructura aporta la configuración del hosting.

| Sección | IDs L1/L2 (PENDING) | IDs L3 (fuera del objetivo; PENDING) |
| --- | --- | --- |
| V3.1 — Web Frontend Security Documentation | — | v5.0.0-3.1.1 |
| V3.2 — Unintended Content Interpretation | v5.0.0-3.2.1, v5.0.0-3.2.2 | v5.0.0-3.2.3 |
| V3.3 — Cookie Setup | v5.0.0-3.3.1, v5.0.0-3.3.2, v5.0.0-3.3.3, v5.0.0-3.3.4 | v5.0.0-3.3.5 |
| V3.4 — Browser Security Mechanism Headers | v5.0.0-3.4.1, v5.0.0-3.4.2, v5.0.0-3.4.3, v5.0.0-3.4.4, v5.0.0-3.4.5, v5.0.0-3.4.6 | v5.0.0-3.4.7, v5.0.0-3.4.8 |
| V3.5 — Browser Origin Separation | v5.0.0-3.5.1, v5.0.0-3.5.2, v5.0.0-3.5.3, v5.0.0-3.5.4, v5.0.0-3.5.5 | v5.0.0-3.5.6, v5.0.0-3.5.7, v5.0.0-3.5.8 |
| V3.6 — External Resource Integrity | — | v5.0.0-3.6.1 |
| V3.7 — Other Browser Security Considerations | v5.0.0-3.7.1, v5.0.0-3.7.2 | v5.0.0-3.7.3, v5.0.0-3.7.4, v5.0.0-3.7.5 |

### V4 — API and Web Service

Revisar contratos, métodos, tipos de contenido y límites de la API. GraphQL y WebSocket requieren confirmar uso.

| Sección | IDs L1/L2 (PENDING) | IDs L3 (fuera del objetivo; PENDING) |
| --- | --- | --- |
| V4.1 — Generic Web Service Security | v5.0.0-4.1.1, v5.0.0-4.1.2, v5.0.0-4.1.3 | v5.0.0-4.1.4, v5.0.0-4.1.5 |
| V4.2 — HTTP Message Structure Validation | v5.0.0-4.2.1 | v5.0.0-4.2.2, v5.0.0-4.2.3, v5.0.0-4.2.4, v5.0.0-4.2.5 |
| V4.3 — GraphQL | v5.0.0-4.3.1, v5.0.0-4.3.2 | — |
| V4.4 — WebSocket | v5.0.0-4.4.1, v5.0.0-4.4.2, v5.0.0-4.4.3, v5.0.0-4.4.4 | — |

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

### V7 — Session Management

Revisar emisión, renovación, caducidad y revocación de sesión; contrastar estado de UI y sesiones Redis/DB.

| Sección | IDs L1/L2 (PENDING) | IDs L3 (fuera del objetivo; PENDING) |
| --- | --- | --- |
| V7.1 — Session Management Documentation | v5.0.0-7.1.1, v5.0.0-7.1.2, v5.0.0-7.1.3 | — |
| V7.2 — Fundamental Session Management Security | v5.0.0-7.2.1, v5.0.0-7.2.2, v5.0.0-7.2.3, v5.0.0-7.2.4 | — |
| V7.3 — Session Timeout | v5.0.0-7.3.1, v5.0.0-7.3.2 | — |
| V7.4 — Session Termination | v5.0.0-7.4.1, v5.0.0-7.4.2, v5.0.0-7.4.3, v5.0.0-7.4.4, v5.0.0-7.4.5 | — |
| V7.5 — Defenses Against Session Abuse | v5.0.0-7.5.1, v5.0.0-7.5.2 | v5.0.0-7.5.3 |
| V7.6 — Federated Re-authentication | v5.0.0-7.6.1, v5.0.0-7.6.2 | — |

### V8 — Authorization

Revisar autorización por objeto y operación en servidor con dos identidades; los guards del cliente solo controlan navegación.

| Sección | IDs L1/L2 (PENDING) | IDs L3 (fuera del objetivo; PENDING) |
| --- | --- | --- |
| V8.1 — Authorization Documentation | v5.0.0-8.1.1, v5.0.0-8.1.2 | v5.0.0-8.1.3, v5.0.0-8.1.4 |
| V8.2 — General Authorization Design | v5.0.0-8.2.1, v5.0.0-8.2.2, v5.0.0-8.2.3 | v5.0.0-8.2.4 |
| V8.3 — Operation Level Authorization | v5.0.0-8.3.1 | v5.0.0-8.3.2, v5.0.0-8.3.3 |
| V8.4 — Other Authorization Considerations | v5.0.0-8.4.1 | v5.0.0-8.4.2 |

### V10 — OAuth and OIDC

Condicional: confirmar OAuth/OIDC y el rol de cada integración antes de evaluar; la presencia de JWT o LDAP no demuestra OAuth/OIDC.

| Sección | IDs L1/L2 (PENDING) | IDs L3 (fuera del objetivo; PENDING) |
| --- | --- | --- |
| V10.1 — Generic OAuth and OIDC Security | v5.0.0-10.1.1, v5.0.0-10.1.2 | — |
| V10.2 — OAuth Client | v5.0.0-10.2.1, v5.0.0-10.2.2 | v5.0.0-10.2.3 |
| V10.3 — OAuth Resource Server | v5.0.0-10.3.1, v5.0.0-10.3.2, v5.0.0-10.3.3, v5.0.0-10.3.4 | v5.0.0-10.3.5 |
| V10.4 — OAuth Authorization Server | v5.0.0-10.4.1, v5.0.0-10.4.2, v5.0.0-10.4.3, v5.0.0-10.4.4, v5.0.0-10.4.5, v5.0.0-10.4.6, v5.0.0-10.4.7, v5.0.0-10.4.8, v5.0.0-10.4.9, v5.0.0-10.4.10, v5.0.0-10.4.11 | v5.0.0-10.4.12, v5.0.0-10.4.13, v5.0.0-10.4.14, v5.0.0-10.4.15, v5.0.0-10.4.16 |
| V10.5 — OIDC Client | v5.0.0-10.5.1, v5.0.0-10.5.2, v5.0.0-10.5.3, v5.0.0-10.5.4, v5.0.0-10.5.5 | — |
| V10.6 — OpenID Provider | v5.0.0-10.6.1, v5.0.0-10.6.2 | — |
| V10.7 — Consent Management | v5.0.0-10.7.1, v5.0.0-10.7.2, v5.0.0-10.7.3 | — |

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

### V17 — WebRTC

Condicional: confirmar uso de WebRTC, TURN, medios y señalización. Sin ese inventario no declarar NOT_APPLICABLE.

| Sección | IDs L1/L2 (PENDING) | IDs L3 (fuera del objetivo; PENDING) |
| --- | --- | --- |
| V17.1 — TURN Server | v5.0.0-17.1.1 | v5.0.0-17.1.2 |
| V17.2 — Media | v5.0.0-17.2.1, v5.0.0-17.2.2, v5.0.0-17.2.3, v5.0.0-17.2.4 | v5.0.0-17.2.5, v5.0.0-17.2.6, v5.0.0-17.2.7, v5.0.0-17.2.8 |
| V17.3 — Signaling | v5.0.0-17.3.1, v5.0.0-17.3.2 | — |

## Evidencia a completar por requisito

- ID oficial, aplicabilidad justificada y responsable confirmado.
- Commit y componente exactos; versión Core consumida cuando corresponda.
- Ambiente y configuración efectiva, sin incluir secretos ni PII.
- Método reproducible, resultado, fecha y enlace a evidencia/test/configuración.
- Estado sustentado por evidencia y, si existe un hallazgo, impacto y propuesta documental para una futura remediación.

No se ejecutaron builds, tests de aplicación ni escaneos en esta etapa. La validación de entrega cubre IDs, niveles, cobertura documental y diff Markdown. No se modifican contratos HTTP, configuración, dependencias, lógica ni punteros de submódulos.

Esta planificación no amplía el piloto automatizado del monorepo. Las revisiones OWASP Top 10 existentes conservan su alcance y estados; no se importan como PASS de ASVS.

Copia documental para coordinación del monorepo. La base del subtree puede diferir del clone original; la evidencia futura debe fijar el commit del componente evaluado.
