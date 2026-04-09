---
name: audit-doc-coverage
description: Detecta faltantes, duplicados y huecos de mantenimiento documental en un repositorio.
argument-hint: "[repo, area o diff opcional]"
kind: diagnostic
---

<!-- ai-toolkit:toolkit profile=base path=.github/skills/toolkit/audit-doc-coverage/SKILL.md -->

# Audit Doc Coverage

Usa esta skill cuando necesites medir deuda documental o revisar si un cambio dejo documentacion suficiente.

## Proceso

1. Identifica el tipo de repo y sus documentos base esperados.
2. Releva documentos presentes, faltantes, duplicados u obsoletos.
3. Contrasta scripts, workflows, arquitectura y superficie publica contra la documentacion real.
4. Prioriza huecos por impacto operativo, arquitectonico o de consumo.

## Salida esperada

- hallazgos criticos
- hallazgos importantes
- mejoras recomendadas
- roadmap corto de correccion
