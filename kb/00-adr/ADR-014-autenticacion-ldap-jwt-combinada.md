---
status: accepted
owner: rubino-f
updated: 2026-09-17
---

# ADR-014 — Autenticación combinada: LDAP + JWT/refresh-token con cookies seguras

> **Retrospectivo, confianza `medium`.** El cambio y su alcance de código son claros; la separación de responsabilidades entre ambos mecanismos es inferida, no confirmada. Ver [`kb/60-descubrimiento/ruta-adr-historicos.md`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-004--autenticación-combinada-ldap-a255e12e--jwtrefresh-token-con-cookies-seguras-f1dd554a) — candidato `ARCH-HIST-004`.

## Estado

`accepted` (2026-09-17). Los gaps sobre reparto de responsabilidades y alcance de la cuenta LDAP se cerraron con confirmación del equipo el 2026-09-17.

## Contexto

`api-admisiones` necesitaba autenticar tanto a consumidores existentes (posiblemente staff/sistemas internos vía LDAP) como a un nuevo frontend público de postulantes.

## Decisión

1. `a255e12e` (2026-03-03): agrega soporte de autenticación LDAP.
2. `f1dd554a` (2026-03-09): agrega JWT + refresh token con cookies seguras, sin remover LDAP — ambos mecanismos coexisten desde entonces.

## Consecuencias

- Una sola superficie de autenticación expuesta (JWT en cookie), con LDAP detrás como almacén de credenciales — ver gaps cerrados más abajo.
- La entidad `RefreshToken` se agregó al modelo de datos sin migración versionada visible (ver `ADR-007`, gap de gobernanza de base de datos).
- **Evolución confirmada más adelante**: el JWT termina entregándose exclusivamente vía cookies HttpOnly (no bearer header, ver julio/`ADR-006`); `api-admisiones` además tiene permisos de **escritura** en LDAP (crea usuarios durante el registro, no solo autentica) — hecho confirmado en junio, más sensible que un simple bind de autenticación.

## Gaps cerrados (2026-09-17, confirmación del equipo)

- **Reparto de responsabilidades:** LDAP se usa exclusivamente dentro del flujo de login (bind de credenciales y alta de usuario durante el registro). Ningún endpoint protegido autentica contra LDAP: toda la autorización de la API va por JWT en cookie HttpOnly. No hay dos superficies de autenticación expuestas, sino una sola (JWT) apoyada en LDAP como almacén de credenciales.
- **Alcance de la cuenta de servicio LDAP:** limitada a bind y creación de usuarios dentro de una OU acotada (rama de postulantes). No tiene permisos de escritura sobre el resto del directorio.

## Referencias

- Candidato de descubrimiento: [`ARCH-HIST-004`](../60-descubrimiento/ruta-adr-historicos.md#arch-hist-004--autenticación-combinada-ldap-a255e12e--jwtrefresh-token-con-cookies-seguras-f1dd554a)
- Relacionado: `ADR-006` (confirma escritura en LDAP y JWT solo por cookie), `ADR-010` (evolución del modelo de login), `ADR-011` (2FA agregado sobre este flujo)
