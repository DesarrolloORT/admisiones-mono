---
status: accepted
owner: rubino-f
updated: 2026-09-17
---

# ADR-011 — Autenticación de dos factores (2FA) por email agregada al login

> **Retrospectivo.** El diseño técnico está documentado en los commits; la elección de canal no lo estaba y fue **confirmada por el equipo el 2026-09-17**, elevando la confianza de `medium` a `alta`. Ver [`kb/60-descubrimiento/ruta-adr-historicos.md`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-012--autenticación-de-dos-factores-2fa-por-email-agregada-al-login) — candidato `ARCH-HIST-012`.

## Estado

`accepted` (2026-09-17). Ambos gaps cerrados: el canal por confirmación del equipo, la condicionalidad verificada en código.

## Contexto

El login de `api-admisiones` (LDAP + JWT, ver `ARCH-HIST-004`, luego evolucionado en `ADR-010`) no tenía segundo factor de autenticación.

## Decisión

`f78213bb` (2026-06-03) agrega 2FA vía código por email: `ILoginFlowService` orquesta reCAPTCHA + rate limiting + LDAP + 2FA en un único flujo; `IDosFactoresAuthService` gestiona la sesión de 2FA en Redis (ver `ADR-005`). Commits posteriores (`8af035cd`, `b17ae88d`, `5347aa4d`) agregan reenvío de código, estado intermedio HTTP 202 y email enmascarado en la respuesta.

## Consecuencias

- El login pasa a depender de la entrega de email en tiempo real, aunque solo en la rama de score bajo (ver más abajo: el 2FA es condicional).
- La configuración de seguridad del login queda concentrada en un solo punto (`ILoginFlowService`), positivo para auditoría.

## Motivación del canal (confirmada por el equipo, 2026-09-17)

Email por **barrera de entrada del postulante**: el público es masivo y de interacción de una sola vez con el sistema. Exigirle instalar y configurar una app de autenticación (TOTP) cortaría la conversión del flujo de admisión. El email ya es un dato que el postulante entrega en el registro.

## El 2FA es condicional, no obligatorio (verificado en código, 2026-09-17)

`AppLogic.Authentication/Services/LoginFlowService.cs:131-147`: tras un LDAP exitoso se compara el score de reCAPTCHA contra `RECAPTCHA_SCORE` (default `0.5`).

- Score **por encima** del mínimo → se emiten los tokens directamente, **sin 2FA**.
- Score **igual o por debajo** → se inicia 2FA por email (`:173`) y se responde HTTP 202 sin emitir tokens.

Es decir: el 2FA es un **step-up condicional disparado por señal de riesgo de reCAPTCHA**, no un segundo factor universal. Los tokens recién se emiten después de validar el código, nunca antes (comentario explícito en `:135` y `:172`).

**Caso borde a tener presente:** si el score es bajo y la persona **no tiene email registrado**, el acceso se **deniega** con `AUTH_2FA_NO_EMAIL` (HTTP 422, `:149-162`). Una persona sin email en `T_PERSONA` puede quedar sin poder entrar ante un score bajo.

## Gaps abiertos

- Definir qué pasa operativamente con personas sin email registrado que caen en score bajo (ver caso borde).
- Con Redis caído el flujo de 2FA responde HTTP 500 — ver `ADR-005`.

## Referencias

- Candidato de descubrimiento: [`ARCH-HIST-012`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-012--autenticación-de-dos-factores-2fa-por-email-agregada-al-login)
- Relacionado: `ADR-005` (Redis), `ADR-009` (hardening de seguridad pública)
