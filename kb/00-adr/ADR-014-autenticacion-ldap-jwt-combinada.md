---
status: draft
owner: rubino-f
updated: 2026-09-15
---

# ADR-014 — Autenticación combinada: LDAP + JWT/refresh-token con cookies seguras

> **Retrospectivo, confianza `medium`.** El cambio y su alcance de código son claros; la separación de responsabilidades entre ambos mecanismos es inferida, no confirmada. Ver [`kb/60-descubrimiento/ruta-adr-historicos.md`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-004--autenticación-combinada-ldap-a255e12e--jwtrefresh-token-con-cookies-seguras-f1dd554a) — candidato `ARCH-HIST-004`.

## Estado

`draft` — pendiente de validación del equipo, en particular sobre qué endpoints usa cada mecanismo hoy.

## Contexto

`api-admisiones` necesitaba autenticar tanto a consumidores existentes (posiblemente staff/sistemas internos vía LDAP) como a un nuevo frontend público de postulantes.

## Decisión

1. `a255e12e` (2026-03-03): agrega soporte de autenticación LDAP.
2. `f1dd554a` (2026-03-09): agrega JWT + refresh token con cookies seguras, sin remover LDAP — ambos mecanismos coexisten desde entonces.

## Consecuencias

- Dos superficies de autenticación para auditar en seguridad.
- La entidad `RefreshToken` se agregó al modelo de datos sin migración versionada visible (ver `ADR-007`, gap de gobernanza de base de datos).
- **Evolución confirmada más adelante**: el JWT termina entregándose exclusivamente vía cookies HttpOnly (no bearer header, ver julio/`ADR-006`); `api-admisiones` además tiene permisos de **escritura** en LDAP (crea usuarios durante el registro, no solo autentica) — hecho confirmado en junio, más sensible que un simple bind de autenticación.

## Gaps

- Confirmar qué endpoints/consumidores usa cada mecanismo (LDAP vs. JWT) y si hay plan de deprecar uno en favor del otro.
- Confirmar el alcance de permisos de la cuenta de servicio LDAP usada por la API (dado que puede crear usuarios, no solo autenticar).

## Referencias

- Candidato de descubrimiento: [`ARCH-HIST-004`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-004--autenticación-combinada-ldap-a255e12e--jwtrefresh-token-con-cookies-seguras-f1dd554a)
- Relacionado: `ADR-006` (confirma escritura en LDAP y JWT solo por cookie), `ADR-010` (evolución del modelo de login), `ADR-011` (2FA agregado sobre este flujo)
