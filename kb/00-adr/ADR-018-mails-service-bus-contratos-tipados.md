---
status: accepted
owner: gabriele
updated: 2026-10-16
---

# ADR-018 — Mails asíncronos por Azure Service Bus con contratos tipados

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entradas del 2026-09-16 (baja de MailORT), 2026-10-09 (merge de `email-integration`, topic `-v2`) y 2026-10-15 (credencial).

## Estado

`accepted` (2026-10-09).

## Contexto

Los mails salían por `Core/MailORT` (SOAP a Office 365) con plantillas HTML armadas en la API.

## Problema

¿Cómo enviar mails sin depender del SOAP ni mantener las plantillas en cada sistema?

## Opciones consideradas

1. **`ORT.Communication.ServiceBus` con HTML armado en la API** (2026-09-16). Primer paso; se mantuvo la interfaz `IEmailSender` propia para no tocar los llamadores.
2. **`ORT.Messaging.ServiceBus` con contratos tipados** (2026-10-09). La API publica el contrato (`AuthenticationMailMessages`) y el worker lo renderiza con TemplatesORT. Elegida.

## Decisión

- `IMessageSender` reemplaza a `IEmailSender`. Se eliminan las plantillas HTML de la API.
- Los contratos tipados van al topic `-v2` (`comunicaciones-topic-dev-v2`), que consume `ORT.Workers.EmailV2`. En el topic legacy, la suscripción por `channel = 'Email'` los toma y el hub los rechaza (`MBM_DA_02`).
- El remitente lo resuelve el consumidor a partir de `Sistema = "ADMISIONES"`; no hay `Mail:From`.
- Credencial del Service Bus por configuración ([ADR-015](ADR-015-secretos-iconfiguration-key-vault.md)).
- Se da de baja el mail de confirmación de la DJ de beca (sale `mailSent` de la respuesta). El PDF de la DJ se mantiene.

## Consecuencias

- `SendAsync` **encola**: falla solo si falla la publicación. Un error de envío posterior es invisible para la API.
- **Breaking con el front:** `mailSent` sale de la confirmación de la DJ.
- `nuget.config` en la raíz con la fuente de GitHub Packages; el Dockerfile recibe el PAT por secret.
- Pendiente: el topic `-v2` de producción.

## Referencias

- `ComunicacionesInfra/terraform/environments/dev/emailv2.tf`
- [ADR-011](ADR-011-2fa-email.md) — 2FA por email
