# 09 — Fix: fuga de `ex.Message` al cliente (R2)

Etapa controlada. **Único objetivo:** eliminar la fuga de detalle de excepción al cliente confirmada en [08](08-validacion-hallazgos.md) §2. Sin refactors, sin tocar RefreshTokenService, EncuestaInicialService, la lógica de `IServiceScopeFactory` ni FondoDeBeca.

## Qué se cambió (patrón aplicado)

En cada `catch` afectado:
1. Se **logueó la excepción real** con `ILogger` (`_logger?.LogError(ex, ...)`) para no perder diagnóstico server-side.
2. Se **quitó el sufijo `: {ex.Message}`** del mensaje devuelto, conservando el prefijo humano ya existente (mensaje genérico).
3. **Se preservaron `ErrorCode` y `HttpCode`** intactos. El comportamiento de éxito no se tocó.

A los servicios que no tenían logger se les agregó un `ILogger<T>? logger = null` **opcional** al constructor: no rompe la construcción existente (tests siguen compilando) ni la resolución por DI (ASP.NET provee el logger). No se cambió ningún otro contrato.

## Archivos modificados

### Producción (6)
| Archivo | Cambios | Logger |
|---------|---------|--------|
| [AuthService.cs](../../WebApiAdmisiones/AppLogic/Services/Autenticacion/AuthService.cs) | 4 catches (`AutenticarUsuarioLDAPAsync`, `RefrescarTokensAsync`, `CompletarPasswordAsync`, `GenerarTokensParaPersonaAsync`) + const `ErrorInesperadoLog` | Ya existía (`_logger`) |
| [RegistroService.cs](../../WebApiAdmisiones/AppLogic/Services/Registro/RegistroService.cs) | 4 catches (`CompletarNuevaPersonaAsync`, `ConfirmarNuevaPersonaAsync`, registrar admisión, `ConfirmarSolicitudAltaAsync`) + const | **Agregado** `ILogger<RegistroService>? = null` |
| [PersonaService.cs](../../WebApiAdmisiones/AppLogic/Services/Personas/PersonaService.cs) | 1 catch (`CambiarPasswordAsync`) | **Agregado** al primary ctor `ILogger<PersonaService>? = null` |
| [PasswordActivationService.cs](../../WebApiAdmisiones/AppLogic/Services/Autenticacion/PasswordActivationService.cs) | 4 catches (`EnviarMailNuevaPersonaAsync`, envío link/recuperación, `ActivarLinkPasswordAsync`, `ValidarSessionToken`) + const | **Agregado** `ILogger<PasswordActivationService>? = null` |
| [RecaptchaService.cs](../../WebApiAdmisiones/WebApiAdmisiones/Security/Captcha/RecaptchaService.cs) | 1 catch (`ValidarConScoreAsync`) | **Agregado** `ILogger<RecaptchaService>? = null` |
| [InscripcionesyPagosApiClient.cs](../../WebApiAdmisiones/AppLogic/ApiClients/InscripcionesyPagosApiClient.cs) | 2 mensajes en `HandleException` (`API_NETWORK`, `API_UNEXPECTED`) | Ya existía y ya logueaba (sin cambios de log) |

**Total: 16 puntos de fuga cerrados** en 6 archivos.

### Tests (2)
| Archivo | Cambio |
|---------|--------|
| [AuthServiceTests.cs](../../WebApiAdmisiones/UnitTesting/Services/AuthServiceTests.cs) | 2 aserciones invertidas: `Contains(inner)` → `DoesNotContain(inner)` (L330 "Database unavailable", L502 "Hash service unavailable") |
| [PersonaServiceTests.cs](../../WebApiAdmisiones/UnitTesting/AppLogic/Services/PersonaServiceTests.cs) | 1 aserción invertida: L826 "LDAP service unavailable" → `DoesNotContain` |

## Antes / después (conceptual)

```csharp
// ANTES  (filtra detalle interno en TODOS los ambientes; saltea la redacción del middleware)
catch (Exception ex)
{
    return OperationResult<T>.IsFailed("CODE_99", nameof(Metodo),
        $"Error al …: {ex.Message}", 500, default!);
}

// DESPUÉS  (log server-side + mensaje genérico; mismo ErrorCode/HttpCode)
catch (Exception ex)
{
    _logger?.LogError(ex, "Error inesperado en {Metodo}", nameof(Metodo));
    return OperationResult<T>.IsFailed("CODE_99", nameof(Metodo),
        "Error al ….", 500, default!);
}
```

Ejemplo real de mensajes:
- `"Error al autenticar usuario: {ex.Message}"` → `"Error al autenticar usuario."`
- `"Error de red: {ex.Message}"` → `"Error de red al comunicarse con el servicio."`
- `"Error al enviar link de {flow.Descripcion}: {ex.Message}"` → `"Error al enviar link de {flow.Descripcion}."` (`flow.Descripcion` es una etiqueta controlada — "activación"/"recuperación" —, no detalle de excepción).

## Tests

Las 3 aserciones invertidas **son** la validación pedida (requisito #5): comprueban que `OperationResult.Message` **no contiene** el texto interno de la excepción, manteniendo la aserción del mensaje genérico y del `ErrorCode`/`HttpCode`. Cubren `AuthService` (2 métodos) y `PersonaService` (1 método). No se agregaron tests nuevos: estos casos ya existían ejercitando la ruta de excepción y solo requerían corregir la expectativa (antes verificaban, incorrectamente, que el detalle interno se exponía).

## Resultado de `dotnet test`

```
Correctas! - Con error: 0, Superado: 811, Omitido: 0, Total: 811 — UnitTesting.dll (net10.0)
```

Build correcto. Los warnings emitidos son preexistentes y ajenos a este cambio (nullabilidad y analizadores xUnit en tests). No se introdujeron errores ni fallos.

## Fuera de alcance (documentado, no modificado)

| Ubicación | Motivo |
|-----------|--------|
| `ExceptionHandlingMiddleware.cs:82,142` | Es el redactor central: ya oculta el mensaje en producción ([08](08-validacion-hallazgos.md) §2). Correcto. |
| `SanitizeAttribute.cs:62` | La excepción fluye por el middleware, que redacta en prod. |
| `JsonSchemaValidationFilter.cs:155` (`new { jex.Message }`) | Baja severidad; describe JSON malformado del propio cliente y no estaba en la tabla de confirmados del doc 08. Queda como decisión documentada. |
| `Core/LdapService/Services/Ldap.cs`, `Core/AzureService/*` (`$"...: {ex.Message}"`) | Submódulo `Core`, fuera del alcance de la auditoría (doc 08 cubrió solo servicios de `WebApiAdmisiones`). Candidato a una etapa futura equivalente sobre `Core`. |

## Correcciones a propagar (pendiente, no crítico)

Sigue vigente la corrección de [08](08-validacion-hallazgos.md) §5 sobre el conteo de tests de `FondoDeBecaController` (0 activos, no ~21) en [01](01-matriz-controllers.md) y [06](06-tests.md).
