# ADR-002 — Evidence before status

## Context
Un estado aislado no explica por qué se considera cumplido un control.

## Decision
Toda evaluación referencia evidencia estructurada con commit, componente, fuente, fecha y resultado. IA/revisión sin evidencia determinística solo produce `NEEDS_REVIEW`.

## Alternatives
Guardar PASS/FAIL manual; confiar directamente en scanners.

## Consequences
Se preserva trazabilidad; recolectar evidencia requiere trabajo adicional.

