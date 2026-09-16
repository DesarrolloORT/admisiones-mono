# Plan OWASP ASVS 5.0.0

Preparación documental solicitada el 2026-09-16. No se aplican fixes.

Catálogo completo: 345 requisitos; 253 de L1/L2 para el objetivo L2 y 92 de L3 fuera del objetivo. Todos PENDING; la aplicabilidad y el cumplimiento requieren evidencia.

- [Catálogo oficial trasladado a Markdown y reparto preliminar](./catalogo.md).
- [Frontend admisiones](./admisiones.md).
- [Backend api-admisiones](./api-admisiones.md).
- [Librería compartida Core](./Core.md).

## Ramas y bases

| Repositorio Git | Base | Rama | Documento nuevo |
| --- | --- | --- | --- |
| admisiones-mono | c2bd42973c9d367d818de6d71a9266c3974e5283 | fix/owasp | security/asvs/planning/README.md y documentos enlazados |
| admisiones (clone original) | 3e250512347e055a6684664e72b2f9a371bbba0a | fix/owasp | security/owasp/ASVS-5.0.0-plan.md |
| api-admisiones (clone original) | 771229f94082f4ecca1ee2b85b22849f89b3b9ae | fix/owasp | security/owasp/ASVS-5.0.0-plan.md |
| Core (clone original) | 7f4e060afadea40fe8c41e8a796fa6a5a1bf723f | fix/owasp | security/owasp/ASVS-5.0.0-plan.md |

Los subtrees admisiones/ y api-admisiones/ comparten el Git del monorepo: no admiten ramas independientes dentro de esas carpetas. Las ramas por repositorio se preparan en los clones originales mediante worktrees aislados bajo el directorio temporal admisiones-owasp-20260916. Los checkouts originales conservan sus cambios sin commit.

Core sigue siendo un submódulo independiente. El plan de Core vive en su clone original; no se actualiza el puntero de Core en ninguna API ni se cambia el checkout del submódulo. La API consume 01239cdf6054dc5a450dcdaa3a867ae3204b61e7, distinto de la base del clone original.

## Límites y coordinación

- La asignación se propone por capítulo/sección y debe afinarse por requisito durante la revisión; una misma exigencia puede requerir evidencia de varios componentes.
- Infraestructura mantiene secretos, TLS, hosting, retención y permisos operativos. No se encontró un repositorio de infraestructura asociado que autorice una rama adicional; la coordinación queda en el monorepo.
- OAuth/OIDC, GraphQL, WebSocket y WebRTC son condicionales: confirmar uso antes de justificar una exclusión.
- El JSON fuente, el piloto, los mapeos de scanners y los estados/evidencias existentes se preservan. La matriz documental no amplía la evaluación automatizada.
- La documentación histórica puede describir distintos mecanismos JWT/cookies/Bearer o distintos hosting. Contrastar el commit y ambiente vigentes antes de tomar esas afirmaciones como evidencia.
- Una futura fase de fixes deberá partir de hallazgos confirmados. Esta entrega incluye únicamente Markdown y commits locales; no publica ramas.

Fuente normativa local: security/asvs/source/OWASP_Application_Security_Verification_Standard_5.0.0_en.json del workspace original (todavía sin commit al iniciar esta tarea). La trazabilidad del catálogo usa el hash de ese archivo; no se incorpora ni modifica en los commits documentales.
