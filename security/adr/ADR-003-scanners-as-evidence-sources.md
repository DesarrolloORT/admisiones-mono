# ADR-003 — SonarQube and ZAP as partial evidence

## Context
Los scanners detectan clases de problemas, pero una regla no demuestra por sí sola un requisito ASVS completo.

## Decision
Importar findings como evidencia normalizada y negativa. Sonar es evidencia de componentes; ZAP es evidencia del sistema y ambiente desplegado. ZAP rechaza `production`.

## Alternatives
Equiparar reglas con controles; instalar otro SAST.

## Consequences
Se reutilizan herramientas existentes sin falsa certeza y se requieren mapeos revisados.

