---
status: accepted
owner: rubino-f
updated: 2026-09-15
---

# ADR-012 — Azure App Configuration como fuente de verdad de configuración de ambiente en el frontend, luego institucionalizado como paquete interno

> **Retrospectivo.** Motivación confirmada por testimonio directo del autor (`rubino-f`) el 2026-09-15. Ver [`kb/60-descubrimiento/ruta-adr-historicos.md`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-013--azure-app-configuration-adoptado-en-el-frontend-para-gestión-de-configuración-por-ambiente) — candidato `ARCH-HIST-013`.

## Estado

`accepted` (2026-09-15).

## Contexto

El frontend `admisiones` mantenía archivos de entorno estáticos (`environment.*.ts`) ignorados por git (por contener configuración/secretos locales). Esto generaba un problema operativo recurrente: cada vez que alguien clonaba el repo, necesitaba que otra persona le pasara manualmente el archivo de configuración para poder compilar, porque git lo ignoraba.

## Decisión

1. `dda35619` (2026-06-16): agrega un script `sync-azure-environment.mjs` que sincroniza **Azure App Configuration** con `environment.ts`.
2. Julio (`0dc3f61e`, `37679bdd`): el script se institucionaliza como paquete npm propio de la organización, `@desarrolloort/azure-env-sync`, reemplazando el script ad-hoc y los paquetes Azure deprecados.

Azure App Configuration pasa a ser **la fuente de verdad** de la configuración de ambiente: los archivos de entorno estáticos quedan completamente reemplazados (no como fallback).

## Motivación (confirmada por el autor)

> "ORT usa todo Microsoft y Azure. La idea de usar esto fue porque siempre teníamos los archivos de configuración locales sin commitear y en cada descarga de un repo teníamos que enviarle a la persona el archivo de configuración para compilar porque estaba ignorado por git. Con esto podemos ya tenerlo todos y actualizado siempre, Azure App Configuration es la fuente de la verdad."

Es decir: no fue una evaluación de alternativas de gestión de configuración — fue la opción natural dado que ORT ya opera enteramente sobre el ecosistema Microsoft/Azure, y resolvía un problema operativo concreto (onboarding manual de configuración local).

## Consecuencias

- Se elimina la fricción de onboarding de configuración local: cualquiera que clone el repo obtiene la configuración vigente sin pedirla a otra persona.
- El patrón subió de "solución puntual de `admisiones`" a **estándar de plataforma para todos los frontends de ORT**: confirmado por el autor que `admisiones` fue el primer repo en adoptarlo, y que el resto de los frontends de la organización están migrando hacia `@desarrolloort/azure-env-sync`.
- Nueva dependencia operativa: si Azure App Configuration no está disponible, ningún desarrollador puede obtener configuración de ambiente actualizada (no hay fallback estático).

## Referencias

- Candidato de descubrimiento: [`ARCH-HIST-013`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-013--azure-app-configuration-adoptado-en-el-frontend-para-gestión-de-configuración-por-ambiente)
- Relacionado: `ADR-008` (mismo patrón organizacional de herramientas/starters compartidos entre frontends de ORT)
