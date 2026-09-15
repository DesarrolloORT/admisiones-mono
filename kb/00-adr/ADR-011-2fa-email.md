---
status: draft
owner: rubino-f
updated: 2026-09-15
---

# ADR-011 — Autenticación de dos factores (2FA) por email agregada al login

> **Retrospectivo, confianza `medium`.** El diseño técnico está bien documentado en los commits; la elección de canal (email vs. TOTP/SMS) no está justificada. Ver [`kb/60-descubrimiento/ruta-adr-historicos.md`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-012--autenticación-de-dos-factores-2fa-por-email-agregada-al-login) — candidato `ARCH-HIST-012`.

## Estado

`draft` — pendiente de validación del equipo.

## Contexto

El login de `api-admisiones` (LDAP + JWT, ver `ARCH-HIST-004`, luego evolucionado en `ADR-010`) no tenía segundo factor de autenticación.

## Decisión

`f78213bb` (2026-06-03) agrega 2FA vía código por email: `ILoginFlowService` orquesta reCAPTCHA + rate limiting + LDAP + 2FA en un único flujo; `IDosFactoresAuthService` gestiona la sesión de 2FA en Redis (ver `ADR-005`). Commits posteriores (`8af035cd`, `b17ae88d`, `5347aa4d`) agregan reenvío de código, estado intermedio HTTP 202 y email enmascarado en la respuesta.

## Consecuencias

- El login pasa a depender de la entrega de email en tiempo real como parte del flujo crítico de autenticación.
- La configuración de seguridad del login queda concentrada en un solo punto (`ILoginFlowService`), positivo para auditoría.

## Gaps

- No confirmado por qué se eligió email como segundo factor en vez de una app de autenticación (TOTP) u otro canal.
- No confirmado si 2FA es obligatorio para todos los usuarios o condicional.

## Referencias

- Candidato de descubrimiento: [`ARCH-HIST-012`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-012--autenticación-de-dos-factores-2fa-por-email-agregada-al-login)
- Relacionado: `ADR-005` (Redis), `ADR-009` (hardening de seguridad pública)
