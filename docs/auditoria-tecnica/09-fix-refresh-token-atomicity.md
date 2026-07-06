# 09 — Fix: atomicidad de `RefreshTokenService.SaveRefreshTokenAsync`

Rama: `fix/refresh-token-atomicity`. Corrige el hallazgo R1 confirmado en [08](08-validacion-hallazgos.md) §1. Alcance acotado: solo `RefreshTokenService` + tests dedicados.

## Problema original

`SaveRefreshTokenAsync` persistía en **dos** `SaveChangesAsync` separados:

```csharp
if (existingToken != null)
{
    _context.RefreshTokens.Remove(existingToken);
    await _context.SaveChangesAsync();   // (1) borra el token anterior — COMMIT
}
var refreshTokenEntity = new RefreshToken { … };
_context.RefreshTokens.Add(refreshTokenEntity);
await _context.SaveChangesAsync();       // (2) inserta el nuevo — COMMIT separado
```

Si el segundo `SaveChangesAsync` fallaba (timeout, pérdida de conexión, constraint), el borrado del paso (1) **ya estaba confirmado** y el usuario quedaba **sin ningún refresh token activo**, forzando reautenticación en el siguiente refresh.

## Análisis del modelo (tarea 2)

`RefreshToken` ([entidad](../../WebApiAdmisiones/BusinessLogic/DevartEFCore/DevartEntities/RefreshToken.cs)) tiene **clave primaria compuesta `(CodigoPersona, Sistema)`** (dos `[Key]`; confirmado en el mapeo `ModelContext.cs:2610` → `HasKey("CodigoPersona","Sistema")`). Por diseño solo puede existir **un** token por combinación persona/sistema.

Consecuencia importante: la opción "Remove del anterior + Add del nuevo en un único `SaveChangesAsync`" (tarea 3) **no es viable**. Al borrar la entidad existente queda marcada como `Deleted` pero **sigue rastreada** por el change tracker con su clave; al hacer `Add` de una nueva instancia con la **misma** clave, EF Core lanza `InvalidOperationException: The instance of entity type 'RefreshToken' cannot be tracked because another instance with the same key value … is already being tracked`.

## Solución aplicada

Como la clave es la misma en ambos casos, "reemplazar el token" equivale a un **upsert sobre el mismo registro**: si existe, se actualizan sus campos en el lugar; si no, se inserta. Todo se persiste con **un único `SaveChangesAsync`**.

```csharp
var token = await _context.RefreshTokens
    .FirstOrDefaultAsync(rt => rt.CodigoPersona == codigoPersona && rt.Sistema == sistema);

if (token == null)
{
    token = new RefreshToken { CodigoPersona = codigoPersona, Sistema = sistema };
    _context.RefreshTokens.Add(token);
}

token.TokenHash = tokenHash;
token.ExpiresAt = expiresAt;
token.CreatedAt = DateTime.UtcNow;
token.IsActive = "SI";
token.RevokedAt = null;
token.RemplaceByTokenId = null;
token.FechaIngreso = DateTime.Now;
token.HoraIngreso = DateTime.Now.ToString("HHmmss");
token.UsuarioIngreso = codigoPersona.ToString();

await _context.SaveChangesAsync();   // único punto de escritura
```

El estado final observable es idéntico al del código anterior (un único registro activo con el hash/vencimiento nuevos), por lo que el comportamiento del login y del refresh no cambia.

### Por qué es atómica

- Hay **un solo** `SaveChangesAsync`, que EF Core ejecuta como **una única sentencia** (`UPDATE` si el registro existía, `INSERT` si no) dentro de una transacción implícita del proveedor. No hay un estado intermedio en el que el token anterior ya se haya borrado y el nuevo aún no exista.
- Si esa escritura falla, **no se aplica nada**: el registro anterior queda intacto (mismo hash, mismo estado). Nunca se pierde el token.
- Se evita además el conflicto de identidad del change tracker (no hay Delete+Add de la misma clave), por lo que **no se necesita** una transacción explícita con `BeginTransactionAsync` (tarea 4): el upsert de una sola fila ya es atómico y, a diferencia de la transacción, es verificable con el proveedor EF Core InMemory que usa el proyecto de tests.

## Archivos modificados

| Archivo | Cambio |
|---------|--------|
| [DataAccess/Services/RefreshTokenService.cs](../../WebApiAdmisiones/DataAccess/Services/RefreshTokenService.cs) | `SaveRefreshTokenAsync` reescrito como upsert de una sola escritura + comentario que documenta la clave compuesta y la atomicidad. Se quitó `[ExcludeFromCodeCoverage]` (y el `using System.Diagnostics.CodeAnalysis`) para que la clase, ahora con tests dedicados, cuente en la cobertura. **No** se tocaron `ValidateRefreshTokenAsync`, `GetCodigoPersonaByRefreshTokenAsync` ni `RevokeRefreshTokenAsync`. Contrato público (`IRefreshTokenService`) sin cambios. |
| [UnitTesting/DataAccess/RefreshTokenServiceTests.cs](../../WebApiAdmisiones/UnitTesting/DataAccess/RefreshTokenServiceTests.cs) | Nuevo. 7 tests con EF Core InMemory sobre el `ModelContext` real. |

## Tests agregados (7)

Usan el proveedor InMemory (`Microsoft.EntityFrameworkCore.InMemory`, ya referenciado) sobre el `ModelContext` real, con un nombre de base único por test.

| Test | Verifica |
|------|----------|
| `SaveRefreshTokenAsync_WhenNoPreviousToken_AddsSingleActiveToken` | Sin token previo: se inserta uno; queda exactamente 1 para (CodigoPersona, Sistema); `TokenHash`/`ExpiresAt` correctos; `CreatedAt` reciente; `IsActive="SI"`; `RevokedAt` null. |
| `SaveRefreshTokenAsync_WhenPreviousTokenExists_ReplacesItKeepingSingleRow` | Con token previo: se reemplaza; queda exactamente 1; el hash activo es el nuevo. |
| `SaveRefreshTokenAsync_WhenSaveFails_PreservesPreviousTokenAndThrows` | Con un `ModelContext` que lanza en `SaveChangesAsync`: la operación falla de forma controlada (propaga excepción) y el token anterior **no se pierde** (sigue con su hash original). |
| `GetCodigoPersonaByRefreshTokenAsync_WhenValidActiveNotExpired_ReturnsCodigoPersona` | Token activo y vigente → devuelve el `CodigoPersona`. |
| `GetCodigoPersonaByRefreshTokenAsync_WhenExpired_ReturnsNull` | Token vencido → null. |
| `GetCodigoPersonaByRefreshTokenAsync_WhenRevoked_ReturnsNull` | Token con `IsActive="NO"` → null. |
| `RevokeRefreshTokenAsync_MarksRevoked_WithoutPhysicalDelete` | Revocar marca `IsActive="NO"` y setea `RevokedAt`, **sin** borrar físicamente el registro. |

El test de fallo (#3) es la validación central del fix: reproduce el escenario que antes dejaba al usuario sin token y demuestra que ahora el token anterior se conserva.

## Resultado de `dotnet test`

```
Correctas! - Con error: 0, Superado: 818, Omitido: 0, Total: 818 — UnitTesting.dll (net10.0)
```

811 preexistentes + 7 nuevos = 818. Sin regresiones. Los warnings del build son preexistentes y ajenos a este cambio (nullabilidad y analizadores xUnit en otros tests).

## Riesgos restantes / a confirmar

- **Nota de mapeo del modelo (nomenclatura):** la columna `RemplaceByTokenId` (`REMPLACE_BY_TOKEN_ID`) tiene un typo (“Remplace”/“Byid”). El servicio no la usa. **No se corrige en esta rama** (implicaría tocar entidad autogenerada + esquema Oracle). Solo se documenta.
- **`IsActive` como `VARCHAR2(2)` "SI"/"NO"** en lugar de un booleano: patrón legacy Oracle. Fuera de alcance; documentado en [02](02-matriz-services.md).
- **Sin historial ni detección de reuso de refresh token:** el token anterior se sobrescribe (upsert), igual que antes se borraba. No se guarda historial de tokens revocados por rotación, por lo que no hay detección de reuso de token robado. Es una mejora de seguridad **fuera del alcance** de esta rama; se deja anotada como hallazgo separado.
- **Transacciones e InMemory:** los tests usan InMemory, que no ejecuta transacciones reales. No aplica aquí porque la solución no depende de transacciones explícitas (la atomicidad viene del único `SaveChangesAsync`). Si en el futuro se quisiera probar el comportamiento transaccional real sobre relacional, habría que usar SQLite/relacional.
- **`ValueGeneratedOnAdd` de `FechaIngreso`/`HoraIngreso`** (defaults por SQL Oracle): el servicio los setea explícitamente, por lo que el comportamiento no depende de los defaults del proveedor. Sin cambios.
