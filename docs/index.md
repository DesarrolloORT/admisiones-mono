---
slug: /
title: Admisiones
description: Portada documental del proyecto Admisiones para ORT Docs Hub.
---

# Admisiones

Admisiones es el frontend primario de los flujos de admision: inicio de sesion,
registro, recuperacion/activacion de cuenta e inscripciones. La documentacion
funcional vive en este repositorio y el backend se referencia desde
`DesarrolloORT/api-admisiones`.

## Documentacion base

- [Setup](./SETUP.md)
- [Workflow](./WORKFLOW.md)
- [Arquitectura](./ARCHITECTURE.md)
- [Estandares frontend](./BEST-PRACTICES.md)
- [Accesibilidad](./ACCESSIBILITY.md)
- [E2E guardrails](./E2E-GUARDRAILS.md)

## Flujos principales

- [Login](./flujos/login.md)
- [Registro](./flujos/registro.md)
- [Inscripciones](./flujos/inscripciones.md)

## Repositorios

- [Frontend Admisiones](https://github.com/DesarrolloORT/admisiones)
- [Backend api-admisiones](https://github.com/DesarrolloORT/api-admisiones)
- [README API](https://github.com/DesarrolloORT/api-admisiones/blob/main/README.md)

## Ambientes

| Ambiente         | URL                                                   |
| ---------------- | ----------------------------------------------------- |
| Produccion       | TODO                                                  |
| Preproduccion    | TODO                                                  |
| Sitio documental | URL prevista: https://ort-docs.ort.edu.uy/admisiones/ |

## Contratos API

No se duplica OpenAPI en esta documentacion. Los contratos tecnicos se consumen
desde Swagger/OpenAPI cuando este disponible y se regeneran en frontend con
`npm run update-api`.
