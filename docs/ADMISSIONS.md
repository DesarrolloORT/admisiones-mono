---
slug: /
title: Admisiones
description: Mapa de la documentación técnica y funcional del proyecto Admisiones.
---

# Admisiones

Esta documentación describe el comportamiento vigente del frontend y backend.
Los archivos Markdown viven junto al código y se actualizan en el mismo cambio
que modifica un flujo.

## Flujos funcionales

- [Inicio de sesion](./LOGIN-FLOW.md): credenciales, 2FA, refresh de token y rutas protegidas.
- [Registro punta a punta](./REGISTER-FLOW.md): decisión del tipo de alta, campos, contratos y efectos.
- [Código backend del registro](https://github.com/DesarrolloORT/api-admisiones/tree/6161d5deb2e5ef9d4a84930013481196a6fd5e1d): implementación verificada del flujo.
- [Inscripciones](../src/app/features/inscriptions/INSCRIPCIONES-FLOW.md): pasos, campos condicionales, payloads y casos borde.

## Arquitectura

- [Arquitectura general](./ARCHITECTURE.md): capas, responsabilidades y flujo de datos.
- [Anatomía del proceso de inscripción](../src/app/features/inscriptions/README.md): stores, fachadas y navegación entre pasos.
- [Buenas prácticas](./BEST-PRACTICES.md): convenciones Angular y contratos de API.
- [Baseline de documentacion](./DOCUMENTATION-GUIDELINES.md): como documentar features front/back mas alla de OpenAPI.

Las referencias a Figma se agregan únicamente con enlaces verificados al nodo
exacto; el portal no incrusta ni sincroniza archivos de diseño.
