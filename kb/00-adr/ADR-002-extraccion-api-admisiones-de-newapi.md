---
status: accepted
owner: rubino-f
updated: 2026-09-17
---

# ADR-002 — Extracción de `api-admisiones` (`WebApiAdmisiones`) desde la plantilla genérica multi-sistema `NewApi`

> **Retrospectivo.** Reconstruido a partir del historial de commits de `api-admisiones` (marzo de 2026). Confianza **alta**: la motivación está explícita en el propio mensaje de commit de la decisión (`0324a3b4`), no es inferencia. Ver [`kb/60-descubrimiento/ruta-adr-historicos.md`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-005--extracción-de-api-admisiones-webapiadmisiones-desde-una-plantilla-genérica-multi-sistema-newapi-compartida-con-empleosfuncionariosgestión) — candidato `ARCH-HIST-005`.

## Estado

`accepted` (2026-09-17).

## Contexto

El backend arrancó (commit inicial `56c5536c`, 2026-03-02) como una plantilla genérica bajo `NewApi/`, con lógica consciente de "sistema origen" para CORS, logging y manejo de excepciones. Esa plantilla estaba pensada para servir a más de un sistema de ORT: además de Admisiones, el código y la infraestructura (CI, Dockerfile, labeler) nombraban explícitamente a **Empleos**, **Funcionarios**, **Gestión** y **FichaDePersona**.

## Problema

Mantener un único backend multi-sistema agrega complejidad (lógica de distinción por sistema origen, CORS más permisivo, superficie de seguridad y de mantenimiento compartida) cuando, en la práctica, cada sistema tiene su propio ciclo de vida y necesidades.

## Opciones consideradas

1. **Mantener el backend multi-sistema**, agregando Admisiones como un "sistema origen" más junto a Empleos/Funcionarios/Gestión. No elegida.
2. **Extraer y especializar un backend dedicado a Admisiones**, eliminando el soporte a los demás sistemas del código de este repo (opción elegida).

No hay evidencia de una tercera alternativa evaluada (p. ej. mantener el multi-sistema pero con feature flags, o separar por configuración en vez de por código).

## Decisión

Restringir `api-admisiones` (`WebApiAdmisiones`) a servir exclusivamente al sistema Admisiones, eliminando la lógica y el código de los demás sistemas:

1. `0324a3b4` (2026-03-13): elimina soporte a Funcionarios y Gestión, restringe CORS a dominios de Admisiones, simplifica lógica de "sistema origen" y logging.
2. `5341b92b` / `9966521b` (2026-03-24): migran Dockerfile, CI/CD, labeler y documentación de `Empleos`/`FichaDePersona` a `WebApiAdmisiones`.
3. `7e90fcce` (2026-03-26): elimina por completo la carpeta `NewApi/` (el scaffold genérico original).

**Motivación (explícita en el commit `0324a3b4`):** simplificar el código y la documentación sirviendo un único sistema, en vez de cuatro.

## Consecuencias

- Superficie de seguridad reducida: CORS y `CurrentUserService` ya solo reconocen Admisiones, no otros sistemas de origen.
- CI/CD, Dockerfile y documentación quedan alineados a un único propósito, simplificando los pipelines.
- **Gap abierto:** no hay evidencia en este repo de si Empleos, Funcionarios y Gestión recibieron cada uno su propio backend extraído de la misma plantilla `NewApi`, o si esos sistemas siguen dependiendo de la plantilla original en otro repositorio. Preguntar al equipo si existe (o existió) un repo `NewApi` del que `api-admisiones` fue forkeado, y qué pasó con los otros sistemas.
- Sin gap de base de datos ni de análisis funcional específico en esta decisión — es alcance/estructura de API, no esquema de datos ni reglas de negocio.

## Referencias

- Candidato de descubrimiento: [`ARCH-HIST-005`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-005--extracción-de-api-admisiones-webapiadmisiones-desde-una-plantilla-genérica-multi-sistema-newapi-compartida-con-empleosfuncionariosgestión)
- Commits fuente: `0324a3b4fed6852ec89e19640be6113e2f2d0f68`, `5341b92be485bcbc84f477cd44808032be1a20e0`, `9966521ba5f8e2590437635ee3ee7b6489262433`, `7e90fccebfcffe5894af6819a82e1807d3c40c71`
