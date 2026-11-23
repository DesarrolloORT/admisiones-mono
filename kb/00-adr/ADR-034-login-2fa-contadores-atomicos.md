---
status: accepted
owner: gabriele
updated: 2026-11-13
---

# ADR-034 — Login y 2FA: los intentos se reservan atómicamente antes de validar

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entradas del 2026-10-30 (L-03, L-04/AUTH-11, L-05), 2026-11-13 08:43 (SEG-L1-01) y 2026-11-13 10:11/11:07 (AUTH-02, AUTH-15).

## Estado

`accepted` (2026-10-30). AUTH-02 y AUTH-15 quedan como **riesgos aceptados** (2026-11-13).

## Contexto

La revisión de seguridad del login (`docs/REVISION-SEGURIDAD-LOGIN.md`, no versionado en el monorepo) y la auditoría previa al pentest encontraron contadores que se leían, se comparaban y recién después se incrementaban. Con requests en paralelo, el límite no se cumplía:

| Hallazgo | Problema |
|---|---|
| L-04 | Los fallos de credenciales se sumaban recién después de LDAP. Una ráfaga desde varias IPs leía "quedan 30" y pasaba entera. |
| L-03 | El 2FA sumaba `Attempts` en memoria y reescribía el JSON. N códigos en paralelo veían el mismo valor. |
| L-05 | La clave IP+documento usaba el tipo de documento crudo: `"CI"`, `" CI"` y `"CI "` tenían 5 intentos cada una. |
| SEG-L1-01 | Dos requests en el mismo milisegundo pisaban el mismo miembro del sorted set. |

## Decisión

- **Reservar antes de validar.** Los contadores de fallos del login se reservan con `IsAllowedAsync`, un script Lua atómico, **antes** de LDAP. Se reserva primero la IP y después el documento, para que una IP ya bloqueada no gaste el contador del documento de otra persona.
- **2FA atómico.** El contador pasa a su propia clave, `2fa:attempts:{sessionId}`, con `INCR` + `PEXPIRE` en Lua. El intento se cuenta antes de comparar el hash. Si se supera `MaxCodeAttempts`, la sesión se borra aunque el código sea correcto.
- **Clave normalizada.** El tipo de documento pasa por `TextNormalization.ToUpperWithoutAccents`, igual que en la búsqueda de la persona.
- **Miembro único en el sorted set:** `{timestamp}:{guid}`. El score sigue siendo el timestamp.

## Riesgos aceptados

- **AUTH-02:** con un score de captcha bajo, una contraseña mal da 401 y una bien da 202 ("te mandamos un código"), así que la respuesta confirma la contraseña de LDAP. Las alternativas cambian lo que ve el usuario. Mitigan los límites por IP, por IP+documento y el bloqueo del documento (30 fallos en 15 minutos).
- **AUTH-15:** reenviar el código reinicia los intentos del 2FA. No reiniciarlos dejaría un solo intento después de varios errores.

## Consecuencias

- El umbral efectivo no cambia: el request N pasa y el siguiente se rechaza.
- Las sesiones de 2FA vivas al desplegar arrancan con el contador en 0.
- L-06 no cambia: un login exitoso también limpia el contador de la IP.

## Referencias

- [ADR-011](ADR-011-2fa-email.md): 2FA por email.
- [ADR-023](ADR-023-endpoints-anonimos-defaults-seguros.md): bloqueo por documento.
- [ADR-033](ADR-033-redis-fail-open.md): con Redis caído.
