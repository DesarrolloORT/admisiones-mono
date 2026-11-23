---
status: accepted
owner: gabriele
updated: 2026-11-19
---

# ADR-035 — IP del cliente: proxies de confianza declarados por ambiente, con IPs exactas

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entradas del 2026-11-18 09:12/09:18/09:45 (PT-H1), 2026-11-18 10:08 (IP en logs) y 2026-11-19 08:55 (Testing).

## Estado

`accepted` (2026-11-18). Resuelve el pendiente de `KnownProxies` de [ADR-023](ADR-023-endpoints-anonimos-defaults-seguros.md).

## Contexto

`UseForwardedHeaders` solo confiaba en loopback. En desa, la API veía la IP del gateway (`::ffff:10.10.4.1`) y todo el login compartía la misma partición de rate limit. La IP real llegaba en `X-Forwarded-For` después de dos saltos (`10.10.4.1` y `192.168.35.204`). Los logs, en cambio, mostraban el XFF crudo, que el cliente puede inventar.

## Problema

¿En qué saltos se confía para tomar la IP real sin dejar que el cliente la falsifique?

## Decisión

- La sección `ForwardedHeaders:KnownProxies` / `KnownNetworks` de cada `appsettings.{env}.json` define los saltos de confianza (`BuildForwardedHeadersOptions`). Sin la sección, el comportamiento es el de antes: solo loopback.
- **IPs exactas, no la red /24.** Confiar en `10.10.4.0/24` es confiar en cualquier contenedor de esa red. Si el gateway cambia, el síntoma es volver al comportamiento anterior, no una falsificación.
- Con saltos declarados, `ForwardLimit = null`. Así se recorre el XFF de derecha a izquierda mientras el salto sea de confianza. Con el default de 1, la API se habría quedado con `192.168.35.204`, que también es compartida.
- Al arrancar se loguean los saltos de confianza, para diagnosticar desde Loki.
- `ClientIpAddress.Resolve` (logs) devuelve la misma IP que los rate limits. Si queda un XFF sin consumir, el log lo muestra aparte: `IP: x (XFF: …)`.

## Consecuencias

- Estado por ambiente: Development verificado desplegado. Testing configurado con los mismos saltos, falta verificarlo desplegado. Preproduction y Production están sin relevar.
- El límite grueso (10 por IP, cuenta también los exitosos) puede molestar detrás de NATs grandes.
- Cómo relevar un ambiente: README de la API ("Proxy e IP del cliente") y `REVIEW.md` R20.

## Referencias

- [`PENTEST-INTERNO-2026-11.md`](../../api-admisiones/docs/PENTEST-INTERNO-2026-11.md), PT-H1.
- [ADR-043](ADR-043-produccion-cerrado-por-defecto.md): checklist de producción.
