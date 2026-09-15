---
status: draft
owner: rubino-f
updated: 2026-09-15
---

# ADR-001 — Consolidación de `admisiones` y `api-admisiones` en monorepo `admisiones-mono` vía `git subtree`, con `Core` como submódulo

> **Retrospectivo.** Reconstruido a partir del historial de commits y de testimonio directo del autor recogido el 2026-09-15, no de un PR/issue contemporáneo (no existió ninguno). Ver [`kb/60-descubrimiento/ruta-adr-historicos.md`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-001--consolidación-en-monorepo-admisiones-mono-vía-git-subtree-con-core-como-submódulo) — candidato `ARCH-HIST-001`. Confianza: **alta** para la motivación (testimonio directo del mismo autor que hizo los commits); **baja/nula** para alternativas descartadas (no cubierto por el testimonio).

## Estado

`draft` — pendiente de revisión y validación por el equipo. No marcar `accepted` sin esa revisión explícita.

## Contexto

A comienzos de septiembre de 2026, el frontend (`admisiones`) y el backend (`api-admisiones`) vivían en repositorios separados, cada uno con su propio pipeline. `api-admisiones` además dependía de `Core`, una librería backend compartida en su propio repositorio.

El equipo de seguridad necesitaba correlacionar escaneos (SAST/DAST/dependencias) entre frontend y backend, y tener repos separados dificultaba esa correlación: no había un lugar único donde cruzar hallazgos de ambos componentes.

## Problema

¿Cómo permitir que el escaneo y la evidencia de seguridad cubran frontend y backend de forma correlacionada, sin perder el historial de commits de ninguno de los dos repos?

## Opciones consideradas

No hay evidencia (commits, PR, issues) de que se hayan evaluado alternativas formalmente. Opciones que el propio cambio implementado descarta implícitamente, pero cuya evaluación explícita no está documentada:

1. **Mantener repos separados**, correlacionando escaneos por fuera (p. ej. agregando resultados en una herramienta externa). Descartada en la práctica, sin registro de por qué.
2. **Monorepo por `git subtree`, preservando historia** (opción elegida). Permite tener ambos códigos bajo un mismo árbol y una misma pipeline de seguridad, sin descartar commits previos.
3. **Monorepo por import squash** (sin preservar historia commit a commit). No elegida.
4. **Incorporar `Core` también por subtree** en vez de submódulo. No elegida — `Core` se mantuvo como submódulo git separado.

## Decisión

Unificar `admisiones` (frontend) y `api-admisiones` (backend) en un único repositorio, `admisiones-mono`, usando `git subtree` para preservar el historial completo de ambos (commits `0dbd9a10` y `06142d7d`, sobre el root `2ec7478`). `Core` se mantiene como submódulo git independiente (`4bb98603`, `.gitmodules` → `https://github.com/DesarrolloORT/Core.git`), no incorporado por subtree.

**Motivación primaria (driver):** habilitar trazabilidad de seguridad cruzada — poder conectar y correlacionar escaneos entre frontend y backend, algo difícil de lograr con los repos separados.

**Beneficio secundario (descubierto después, no motivación original):** el monorepo también sirve como lugar único para documentar cambios (KB, ADR). Esto se identificó una vez que ya existía el monorepo, no fue parte de la decisión inicial.

## Consecuencias

- El historial de `admisiones` y `api-admisiones` queda preservado commit a commit dentro de `admisiones-mono` (no es un import squash), lo que permite trazar decisiones históricas de ambos repos desde un solo lugar.
- `Core` sigue teniendo ciclo de release propio y separado: cualquier cambio en `Core` requiere actualizar la referencia de submódulo en `api-admisiones`. No hay registro de por qué se decidió así en vez de subtree; queda como gap abierto.
- Se habilita (en commits inmediatamente posteriores, fuera del alcance de este ADR) la construcción de una KB (`7b61f7d1`) y un baseline de seguridad ASVS (`8354b7af`) que cruzan ambos componentes — consistente con la motivación de seguridad, aunque la documentación fue un beneficio identificado después.
- **Cosa extraña a señalar al equipo:** el commit fundacional del monorepo (`2ec7478`) mezcla el scaffolding con dos documentos de auditoría de mensajes de error de frontend (`AUDITORIA-ERRORES.md`, `RONDA-4.md`, relacionados con la branch `admisiones@v1.0.0/fix/apiMessages`) sin relación con la decisión estructural. No es en sí una decisión de arquitectura, pero conviene limpiarlo o documentarlo aparte para no confundir a quien lea el historial fundacional.
- **Gap de base de datos / análisis funcional:** no aplica a este ADR — es una decisión puramente de estructura de repositorio, sin cambios de esquema de datos ni de reglas funcionales. El autor advirtió que "muchos cambios fueron realizados previamente" sin quedar documentados; es esperable que aparezcan, más adelante en el historial de `admisiones`/`api-admisiones`, decisiones de base de datos o funcionales sin contexto recuperable. Eso se evaluará candidato por candidato en los próximos lotes, no en este ADR.

## Referencias

- Candidato de descubrimiento: [`ARCH-HIST-001`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-001--consolidación-en-monorepo-admisiones-mono-vía-git-subtree-con-core-como-submódulo)
- Commits fuente: `2ec7478944611098c95ad30f718ba47c0c8383cd`, `0dbd9a10c9986253c6fd2cbd1db7aafce68fa9c9`, `06142d7dc805dc294a0136b6ab44452ebad6da3e`, `4bb986030f9efe8b0e24d33a79366d0593e97716`
- Baseline de seguridad: [`../../security/`](../../security/)
