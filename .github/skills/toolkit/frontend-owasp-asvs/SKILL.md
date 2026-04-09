---
name: frontend-owasp-asvs
description: Revisa aplicaciones frontend contra controles OWASP ASVS con foco en autenticacion, autorizacion, manejo de datos sensibles, navegacion y consumo de APIs.
argument-hint: "[ruta, modulo o flujo]"
---
<!-- ai-toolkit:toolkit profile=security path=.github/skills/toolkit/frontend-owasp-asvs/SKILL.md -->

# OWASP ASVS para Frontend

Usa esta skill cuando necesites revisar codigo frontend desde una perspectiva de seguridad y devolver hallazgos accionables.

## Cuando usarla

- revisiones de seguridad en componentes, rutas, guards, servicios HTTP o manejo de tokens
- auditorias previas a release
- validacion de cambios sensibles en autenticacion o autorizacion

## Proceso

1. Delimita el flujo o modulo a revisar.
2. Usa la [checklist base](./references/checklist.md) para cubrir controles sin improvisar.
3. Confirma hechos observables en codigo antes de marcar un finding.
4. Clasifica cada hallazgo por severidad y explotabilidad.
5. Si el control depende del backend, deja claro el limite del analisis frontend.

## Salida esperada

- findings priorizados
- evidencia o archivo relevante
- recomendacion concreta de remediacion
