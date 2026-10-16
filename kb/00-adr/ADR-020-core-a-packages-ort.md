---
status: accepted
owner: gabriele
updated: 2026-10-16
---

# ADR-020 — Infraestructura de `Core` a packages `ORT.*`; el dominio se queda en admisiones

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entradas del 2026-09-16 (MailORT), 2026-09-17 (parseo de documentos), 2026-10-06 (teléfonos, `FileValidator`) y 2026-10-07 (LDAP).

## Estado

`accepted` (2026-10-07).

## Contexto

`Core` es un submódulo compartido por cinco repos y no se toca desde admisiones. Mezcla infraestructura (LDAP, mail, teléfonos) con reglas de negocio, y devuelve `OperationResult`.

## Problema

¿Qué sale de `Core`, a dónde va, y qué se queda en admisiones?

## Decisión

Criterio: **lo genérico va a un package de InfraestructuraORT; lo que filtra dominio se queda en admisiones.**

| Pieza | Destino |
|---|---|
| LDAP (`Core/LdapService`) | `ORT.Ldap.Abstraction` + `ORT.Ldap.Soap` 2.0.5, `ILdapService` con `Result` |
| Validador de teléfonos | `ORT.Utilities.Phone` 2.0.5, en inglés |
| Mail (`Core/MailORT`) | `ORT.Messaging.*` ([ADR-018](ADR-018-mails-service-bus-contratos-tipados.md)) |
| `FileValidator` | `AppLogic.Contracts.Files` (no se empaqueta: filtraba dominio, `DeclaracionJurada`) |
| Parseo de cédula uruguaya | Se queda en quien llama, no en `ORT.DocumentIntelligence.*` |

- La configuración la arma admisiones: el package no bindea `IConfiguration`. El secreto de LDAP (`claveHash`) lo aporta `LdapServiceKeyProvider` desde la tabla de parámetros.
- El parseo no se mueve al package: reglas de negocio en infraestructura compartida obligan a publicar y bumpear en todos los repos ante cada cambio.
- Las copias de `Core` no se borran: las siguen usando otros repos.

## Consecuencias

- Desaparecen los puentes `ToResult()`/`ToError()` para LDAP.
- `UserExistsAsync` ya no traga errores: con el directorio caído responde 500 en vez de "el usuario no existe".
- **A verificar:** el `sistema` enviado a LDAP pasa de `EMPLEOSWEB` a `ADMISIONES`; timeout de 10 s por llamada (antes 1 min).
- Los mensajes de error de teléfonos y archivos quedan en inglés dentro de un mensaje en español. Pendiente decidir si se localizan.
- LogicaORT (.NET Framework) no puede consumir los packages (`net10.0`).
- Pendiente: migrar `api-process-automation`, sacar las copias de `Core` y el bug de apellidos de `Core/AzureService`.

## Referencias

- [ADR-014](ADR-014-autenticacion-ldap-jwt-combinada.md) — LDAP + JWT
- [ADR-004](ADR-004-reconocimiento-documentos-azure.md) — reconocimiento de documentos
