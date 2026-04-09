---
name: github-actions-cicd
description: Diagnostica, disena o ajusta workflows de GitHub Actions y flujos CI/CD del repo con cambios acotados y verificables.
argument-hint: "[workflow, job o incidente]"
kind: workflow
---
<!-- ai-toolkit:toolkit profile=github-actions path=.github/skills/toolkit/github-actions-cicd/SKILL.md -->

# GitHub Actions CI/CD

Usa esta skill cuando la tarea involucre `.github/workflows/*.yml`, matrices, permisos, caches, artifacts, releases o fallos de pipelines.

## Cuando usarla

- Fallos de CI, CD o release que dependen de uno o mas jobs.
- Diseno o refactor de workflows, reusable workflows y permisos.
- Optimizacion de caches, artifacts, concurrency, matrices o environments.

## Proceso

1. Delimita workflow, evento, jobs afectados y criterio de exito.
2. Revisa triggers, `permissions`, `needs`, outputs, secretos, matrices y condiciones antes de editar.
3. Prefiere cambios minimos y explicitos en YAML, scripts y acciones versionadas.
4. Verifica impacto en ramas protegidas, artifacts, releases y tiempos de ejecucion.
5. Si el problema depende de secretos, runners o configuracion externa, deja el limite visible.

## Salida esperada

- causa o cuello de botella principal
- cambio propuesto o aplicado por workflow o job
- verificacion con comandos, rutas o corrida esperada
- riesgos, dependencias externas o follow-ups
