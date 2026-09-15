# Instrucciones para agentes

1. Leer primero `kb/README.md`.
2. Consultar `10-dominio/` antes de implementar lógica de negocio.
3. Consultar `00-adr/` antes de cambiar decisiones de arquitectura.
4. Consultar `30-arquitectura/seguridad.md` antes de cambios de autenticación, autorización, secretos, datos sensibles o exposición externa.
5. No asumir que `60-descubrimiento/` es verdadero.
6. No inventar reglas de negocio. Cuando falte conocimiento necesario, señalar explícitamente el gap.
7. Si el análisis del código revela conocimiento relevante no documentado, proponer una actualización de la KB con su evidencia.
8. No modificar silenciosamente ADR aceptados. Si una implementación contradice uno, advertirlo y proponer un nuevo ADR.
9. Mantener documentos pequeños, usar enlaces relativos y respetar los estados definidos en `kb/README.md`.

