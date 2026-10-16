---
status: accepted
owner: gabriele
updated: 2026-10-16
---

# ADR-015 — Secretos por `IConfiguration` y Azure Key Vault con el proveedor estándar

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entradas del 2026-10-15 (merge de `mila/keyvault`, secretos por `IConfiguration`, Service Bus, Document Intelligence).

## Estado

`accepted` (2026-10-15).

## Contexto

Los secretos vivían en variables de entorno con nombres planos (`JWT_SECRET_KEY`, `OracleConnectionStringAdmisiones`, …) y casi todo el código los leía con `Environment.GetEnvironmentVariable`, que no ve lo que carga `IConfiguration`. Se quería pasar los secretos a Azure Key Vault.

## Problema

¿Cómo leer los secretos desde Key Vault sin mantener dos mecanismos ni romper los ambientes que siguen con variables de entorno?

## Opciones consideradas

- **Extensión propia que exporta el vault a variables de entorno.** Se probó y se descartó por no ser estándar.
- **Proveedor estándar `AddAzureKeyVault` + todo por `IConfiguration`.** Elegida ("quiero usar el estándar"). Exige renombrar los secretos del vault, porque el proveedor solo traduce `--` → `:`.

## Decisión

- `Program.cs` usa `AddAzureKeyVault(uri, new DefaultAzureCredential())` solo si `KeyVault:VaultUrl` está definido (sin el guard, `new Uri(null)` tira en los ambientes sin vault y en los tests). Va antes de `ValidateRequiredSecrets()`.
- Todo secreto se lee de `IConfiguration` con clave jerárquica, la misma en las tres fuentes: `Jwt:SecretKey` = env `Jwt__SecretKey` = vault `Jwt--SecretKey`. El mapa viejo → nuevo está en la fuente.
- Los servicios reciben `IConfiguration` por constructor; los tests usan configuración en memoria (`TestConfiguration.Build`) en vez de variables de entorno del proceso.
- Service Bus de mails: credencial propia `ServiceBus:TenantId/ClientId/ClientSecret` (otro tenant que el vault), con fallback a `DefaultAzureCredential`. Las variables `AZURE_*` del servidor quedan solo para entrar al vault.
- Los `DefaultAzureCredential` excluyen managed identity: la API corre en Docker fuera de Azure y el probe a IMDS trababa el arranque minutos.
- `Core/AzureService` (Document Intelligence) también lee de `IConfiguration` (`AzureDocumentIntelligence:*`), primero la variable de entorno. Excepción a la regla de no tocar `Core`, por pedido explícito.

## Consecuencias

- **Breaking de despliegue:** las variables viejas dejan de leerse; cada ambiente pasa a los nombres nuevos o al vault antes de desplegar.
- `AZURE_TENANT_ID/CLIENT_ID/CLIENT_SECRET` no pueden venir del vault: son la credencial para entrar a él.
- Si algún día se despliega en Azure con managed identity, hay que sacar la exclusión.
- Falta definir `KeyVault:VaultUrl` para Testing, Preproduction y Production. Development apunta a `kv-admisiones-develop`.

## Referencias

- [ADR-012](ADR-012-azure-app-configuration-frontend.md) — configuración en el frontend
- [ADR-018](ADR-018-mails-service-bus-contratos-tipados.md) — Service Bus de mails
