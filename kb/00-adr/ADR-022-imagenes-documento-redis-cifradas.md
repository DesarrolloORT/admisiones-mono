---
status: accepted
owner: gabriele
updated: 2026-10-16
---

# ADR-022 — Imágenes de documento en Redis: cifradas, clave hasheada y TTL del flujo

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entradas IDEN-01 del 2026-09-16 (11:17 y 11:36).

## Estado

`accepted` (2026-09-16).

## Contexto

`RedisIdentityDocumentImageCache` guardaba la foto del documento y de la cara como JSON en claro, con el documento en la clave y TTL de 24 h. `KEYS`/`MONITOR` exponían qué documentos estaban en trámite, y un backup de Redis se llevaba las imágenes.

## Problema

¿Cómo proteger datos biométricos en Redis sin perder performance, y con un TTL que no quede desalineado del link de activación?

## Decisión

- **Clave:** `SHA256(tipo:documento)` en hex. Determinística, así que el cache funciona igual.
- **Contenido:** AES-256-GCM (autenticado), nonce de 12 bytes por escritura, en un único blob base64. Secreto `DocumentImages:EncryptionKey` (≥32 bytes), derivado con SHA256. AES-NI hace el costo despreciable.
- **TTL lo decide quien llama**, no la caché. `analyze-attachment` consulta si la persona existe: existente → `PasswordActivation:ExpireHours` (24 h), nueva → `PasswordActivation:NewPersonExpireHours` (3 h). Son las mismas claves que usan los links, así que no se desalinean.

## Opciones descartadas

- TTL fijo de 3 h: una persona existente que tardara más de 3 h perdía las imágenes.
- Bajar `ExpireHours` a 3 h: acortaba también el recupero de contraseña.

## Consecuencias

- Una consulta extra a Oracle por subida de foto; despreciable frente a la llamada a Azure.
- `registro:pending-doc:{tipo}:{doc}` sigue con el documento en claro: no estaba en el alcance.
- Falta confirmar con infra `requirepass`, TLS y backups cifrados de Redis.

## Referencias

- [ADR-005](ADR-005-redis-cache-y-estado.md) — Redis
- [ADR-004](ADR-004-reconocimiento-documentos-azure.md) — reconocimiento de documentos
- [ADR-015](ADR-015-secretos-iconfiguration-key-vault.md) — secretos
