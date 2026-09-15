---
status: draft
owner: rubino-f
updated: 2026-09-15
---

# ADR-006 — Modularización completa de `AppLogic` y traducción de la API a inglés

> **Retrospectivo.** A diferencia de los demás ADR de esta serie, este no se reconstruyó a partir de mensajes de commit inferidos: el propio equipo documentó el diagnóstico, el diseño y el resultado en `api-admisiones/docs/` (`MATRIZ-TRAZABILIDAD.md`, `README-MODULOS.md`, `GLOSARIO-DOMINIO.md`). Confianza **alta**. Ver [`kb/60-descubrimiento/ruta-adr-historicos.md`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-015--modularización-completa-de-applogic-monolito--12-módulos-sin-ciclos-y-traducción-de-toda-la-api-a-inglés-breaking-change-coordinado-con-el-frontend) — candidato `ARCH-HIST-015`.

## Estado

`draft` — pendiente de revisión y validación por el equipo. No marcar `accepted` sin esa revisión explícita.

## Contexto

Una auditoría técnica formal de `WebApiAdmisiones` (2026-07-06, `docs/auditoria-tecnica/` original, luego consolidada) encontró: servicios monolíticos de cientos de líneas con decenas de responsabilidades (`AuthService` 712 líneas, `RegistrationService` 682 líneas), ciclos de dependencia entre módulos lógicos, manejo de errores inconsistente (`OperationResult<T>` usado hasta en validadores puros), mensajes de excepción filtrados al cliente, y código muerto.

## Problema

¿Cómo pasar de un proyecto `AppLogic` monolítico a una arquitectura modular, sin ciclos de dependencia, con responsabilidades claras y sin perder ni una sola de las 39 rutas HTTP ni las 451 propiedades públicas existentes?

## Decisión

1. Auditoría documentada (matrices de controllers/services, dependencias, DI, tests) → plan de refactor multi-fase.
2. Piloto en el módulo más chico (`Tivenos`) para validar el patrón `module-first` (`Interfaces/`, `Services/`, `Dtos/`) antes de aplicarlo a los módulos grandes.
3. División de `AppLogic` en **12 módulos en 6 niveles de dependencia sin ciclos**: `Contracts`, `DevartDtos` (nivel 1) → `Platform`, `Integrations.Tivenos`, `Integrations.EnrollmentsAndPayments` (nivel 2) → `Identity` (nivel 3) → `People`, `Authentication`, `Scholarships` (nivel 4) → `Registration`, `Enrollments` (nivel 5) → `Catalogs` (nivel 6).
4. Los 4 ciclos de dependencia detectados se rompieron con técnicas documentadas caso por caso, incluyendo una inversión de dependencia justificada explícitamente como "límite arquitectónico" (`IPendingRegistrationCompletion`).
5. Los servicios monolíticos (`InscripcionesService`, `PersonService`, `RegistrationService`, `AuthService`) se dividieron en **casos de uso** individuales (`UseCases/`), uno por endpoint.
6. `OperationResult<T>` se retiró de reglas, validadores, mappers y factories — se conserva solo en la firma de los casos de uso (el body HTTP). Cada módulo define un enum de negocio propio + catálogo de error.
7. **Toda la superficie pública de la API se tradujo a inglés**: tipos, propiedades de DTO, payload JSON completo, y rutas HTTP (`Auth/Login` → `auth/login`, kebab-case). Documentado como **breaking change que requiere despliegue coordinado con el frontend**.

## Excepciones documentadas (no descuido)

- Los DTOs de `Integrations.*` que deserializan formatos de sistemas externos quedan en español — traducirlos rompería la deserialización sin dar error de compilación.
- Los mensajes de error van en español; solo los identificadores en inglés.
- Los códigos de error (`GEN_IP_01`, etc.) son contrato opaco, no se renombran.

## Consecuencias

- **Bug real de integración descubierto y corregido**: `T_PERSONA` es una tabla Oracle compartida entre `api-admisiones` y otro sistema de ORT, **"FDP" (Ficha de Persona)**, con caminos de escritura de teléfono independientes y no coordinados (FDP guardaba E.164, admisiones guardaba string crudo). Se corrigió normalizando en los tres puntos de escritura de admisiones, con el teléfono pasando a viajar como objeto estructurado — otro breaking change coordinado con el front.
- Se documentó la existencia de **"LogicaORT"**, un proceso legacy fuera de este repo que migra datos de forma diferida hacia `T_ENCUESTA_INI`. La API tuvo que diseñar su propia máquina de estados (`TEMPORAL`→`CONFIRMADO`→`DEFINITIVO`) para coexistir con ese proceso sin pisarlo, y requirió una migración de datos puntual.
- Deuda conocida y documentada explícitamente: el módulo `Scholarships` (fondo de becas) está implementado y testeado pero ningún controller lo consume — decisión de producto pendiente.
- El frontend (`admisiones`) hizo el mismo movimiento en paralelo: el 2026-08-14 migró su vocabulario interno (dashboard, enrollments, scholarships, auth, catalogs) de español a inglés, en coordinación con los contratos JSON en inglés que expone este ADR.

## Gaps

- **Gobernanza de base de datos**: confirmado con nombre propio que al menos tres sistemas (`api-admisiones`, `FDP`, `LogicaORT`) comparten tablas Oracle sin un dueño único de esquema ni contrato formal. Se recomienda un inventario completo de sistemas que tocan `T_PERSONA`, `T_ENCUESTA_INI` y tablas relacionadas antes de futuros cambios de esquema — esto ya no es una sospecha inferida, es un hecho documentado tras un incidente real.
- Confirmar si la migración de datos del cambio de estado de encuesta inicial ya se ejecutó en producción.
- Confirmar con producto el destino de `Scholarships`/fondo de becas.

## Referencias

- Candidato de descubrimiento: [`ARCH-HIST-015`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-015--modularización-completa-de-applogic-monolito--12-módulos-sin-ciclos-y-traducción-de-toda-la-api-a-inglés-breaking-change-coordinado-con-el-frontend)
- Fuente primaria (backend): `api-admisiones/docs/README-MODULOS.md`, `api-admisiones/docs/MATRIZ-TRAZABILIDAD.md`, `api-admisiones/docs/GLOSARIO-DOMINIO.md`, `api-admisiones/docs/GUIA-ESTILO-CODIGO.md`, `api-admisiones/docs/11-estructura-tivenos-piloto.md`
- Commits fuente: `6aec48c8` (auditoría), `6ec42f6b` (fixes de auditoría), `70cf6192` (arranque del refactor modular), `75038255`, `c8f33a69`
