---
status: accepted
owner: gabriele
updated: 2026-11-19
---

# ADR-045 — Capa de verificación: el agente verifica su cambio, y la seguridad se prueba con pentest interno y fuzzing en memoria

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entradas del 2026-11-17 15:01, 2026-11-18 08:07, 2026-11-18 08:47/08:56 (pentest interno), 2026-11-19 08:24 y 08:40 (fuzzing).

## Estado

`accepted` (2026-11-17).

## Contexto

Buena parte del código lo escribe un agente. El equipo quiere que el humano vea solo las fallas y la evidencia, no que repita cada chequeo. Antes del pentest externo hacía falta cubrir lo evidente con pruebas que se puedan repetir.

## Decisión

- **Skill `verify`** (`.claude/skills/verify/SKILL.md`):
  1. build;
  2. tests;
  3. API levantada con los secretos de `kv-admisiones-develop` por `az login`, sin copiarlos a archivos;
  4. curl;
  5. k6 en `SMOKE=1`.

  Lo que no se pudo correr se reporta como "no ejecutado".
- **Reglas en `AGENTS.md`:**
  - cada commit lleva el trailer `Verified:`;
  - después de 3 rondas fallidas se escala al humano;
  - un test flaky se arregla de raíz o se borra y se registra;
  - un cambio no edita sus propios chequeos.
- **`REVIEW.md`:** disparador → chequeo → origen. Un ítem nuevo entra solo si el defecto apareció en 2 PRs o más, o causó un incidente.
- **Hooks:**
  - `Stop` corre build + tests si hay `.cs` modificados;
  - `PreToolUse` controla el `Verified:` y arranca en modo *observe*.
- **Tests de integración (`ApiFactory`):** levantan la API entera en memoria. Congelan los endpoints anónimos y exigen 401 sin body en todo lo protegido.
- **Pentest interno contra develop:** con dos cuentas de prueba y respaldo previo, Schemathesis y ZAP. Queda como `loadtest/security-smoke.sh`, repetible.
- **Fuzzing en memoria (`InputFuzzingTests`) y no contra develop.** Recorre los 62 endpoints con semillas fijas y 40 mutaciones. Falla ante un 5xx inesperado o un error sin `{ message }`. Contra develop habría hecho falta una persona reseteable o limpiar Oracle con DELETE en una base compartida.

## Consecuencias

- El pentest interno no encontró IDOR, falsificación de sesión ni inyección. Lo que sí encontró se trató en [ADR-031](ADR-031-sesion-cookie-csrf-cors-samesite.md), [ADR-032](ADR-032-revocacion-sesiones.md), [ADR-035](ADR-035-ip-cliente-proxies-confianza.md), [ADR-038](ADR-038-archivos-tipo-por-contenido.md), [ADR-041](ADR-041-datos-personales-en-logs.md) y [ADR-043](ADR-043-produccion-cerrado-por-defecto.md).
- El stub de captcha (`loadtest/captcha-stub.js`) queda en el repo. Sin él, ni `verify` ni k6 pueden loguearse contra una API local.
- **Afuera:**
  - el diferencial contra LogicaORT;
  - branch protection, que configura un admin de GitHub;
  - las reglas de negocio con datos reales en el fuzzing.

## Referencias

- [`PENTEST-INTERNO-2026-11.md`](../../api-admisiones/docs/PENTEST-INTERNO-2026-11.md)
- `api-admisiones/AGENTS.md`, `api-admisiones/REVIEW.md`
