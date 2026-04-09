---
name: angular-app-implementation
description: Implementa cambios Angular multiarchivo siguiendo patrones del workspace, design system y testing minimo.
argument-hint: "[ruta, flujo o feature]"
kind: workflow
---
<!-- ai-toolkit:toolkit profile=angular-advanced path=.github/skills/toolkit/angular-app-implementation/SKILL.md -->

# Angular App Implementation

Usa esta skill cuando el cambio Angular supere el happy path del prompt publico y ya involucre varios archivos coordinados: componente, template, estilos, rutas, servicios o tests.

## Cuando usarla

- pantallas o features nuevas
- refactors UI con wiring Angular real
- integraciones entre componente, rutas, estado y servicios
- cambios donde un solo prompt de implementacion ya queda corto por coordinacion o alcance

## Proceso

1. Delimita flujo, archivos afectados y criterio de exito observable.
2. Revisa patrones Angular equivalentes del workspace antes de proponer nuevas estructuras.
3. Reutiliza design system, servicios, utilidades y contratos existentes antes de crear nuevos.
4. Cierra el wiring minimo coherente entre template, estado, rutas, servicios y tests.
5. Si falta una primitive del design system, infraestructura o contrato, deja el gap explicito.

## Regla clave

- El objetivo es cerrar el flujo Angular solicitado con el minimo cambio coherente, no redisenar la arquitectura del repo.

## Salida esperada

- causa del flujo o del bug a resolver
- cambio aplicado por area o archivo relevante
- verificacion minima ejecutada o pendiente concreta
- riesgos si faltan primitives, datos o contratos del repo
