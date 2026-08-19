---
slug: /
title: Admisiones
description: Catálogo central del comportamiento y las fuentes de Admisiones.
---

# Admisiones — mapa de conocimiento

Este portal es el punto de entrada para comprender Admisiones. No reemplaza las
fuentes ejecutables: indica cuál manda para cada tipo de verdad y enlaza su
evidencia.

## Autoridades

| Pregunta                       | Fuente canónica                                             |
| ------------------------------ | ----------------------------------------------------------- |
| Qué ve y hace la persona       | Páginas de flujo de este portal                             |
| Qué se envía por HTTP          | Swagger/OpenAPI de `api-admisiones`                         |
| Cómo se implementa en frontend | Código y tests de la ref frontend configurada               |
| Qué reglas ejecuta el servidor | Documentación, código y tests de la ref backend configurada |
| Por qué cambió                 | PR y Git; ADR solo para decisiones transversales            |

Refs actuales:

- Frontend: `DesarrolloORT/admisiones@v1.0.0/main`.
- Backend: `DesarrolloORT/api-admisiones@develop`.
- Configuración única: `docs-site/src/config/source-repositories.ts`.

Si dos fuentes se contradicen, la página debe mostrar **Drift detectado** hasta que
ambos repositorios queden alineados.

## Flujos canónicos

| Flujo                                      | `businessId`               | Contenido                                             |
| ------------------------------------------ | -------------------------- | ----------------------------------------------------- |
| [Login](./flujos/login.md)                 | `admisiones.login`         | Credenciales, captcha, 2FA, cookies, refresh y guards |
| [Registro](./flujos/registro.md)           | `admisiones.registro`      | OCR, identidad, alta, activación y contraseña         |
| [Inscripciones](./flujos/inscripciones.md) | `admisiones.inscripciones` | Propuesta, encuesta, identidad, confirmación y pago   |
| [Becas](./flujos/becas.md)                 | `admisiones.becas`         | Catálogo, gating por inscripción previa y postulación |

Cada página sigue la acción desde la UI hasta controller/service backend. OpenAPI
conserva la autoridad sobre rutas y shapes; las páginas explican decisiones,
errores, estados y efectos que el contrato no expresa.

## Arquitectura y operación

- [Arquitectura](./ARCHITECTURE.md)
- [Anatomía de un flujo paso a paso](./arquitectura/flujo-pasos.md)
- [Baseline del knowledge hub](./DOCUMENTATION-GUIDELINES.md)
- [Setup](./SETUP.md)
- [Workflow](./WORKFLOW.md)
- [Estándares frontend](./BEST-PRACTICES.md)
- [Accesibilidad](./ACCESSIBILITY.md)
- [E2E guardrails](./E2E-GUARDRAILS.md)
- [Runbook](./RUNBOOK.md)
- [Releases](./RELEASES.md) — incluye la puerta de exposicion antes de prod

## Mantenimiento

Los agentes leen primero este índice y luego el flujo afectado. Un cambio en los
`sourcePaths` de una página debe actualizarla en el mismo PR o incluir
`docs-none: <motivo>`.

```bash
npm run check:knowledge
npm run test:knowledge
npm run docs:build
```

Git es el log de mantenimiento. No se mantiene una wiki duplicada, una base
vectorial ni un historial manual.
