# 08 — Validación de hallazgos críticos

> Revisión directa contra el código fuente (no contra los resúmenes de [02](02-matriz-services.md)). Cada punto se validó leyendo el archivo completo. **No se modificó código.** Las líneas citadas son exactas a la fecha de esta revisión.

---

## 1. `RefreshTokenService.SaveRefreshTokenAsync`: dos `SaveChangesAsync` sin transacción

**Veredicto: ✅ Confirmado.**

**Evidencia exacta** — [DataAccess/Services/RefreshTokenService.cs:24-53](../../WebApiAdmisiones/DataAccess/Services/RefreshTokenService.cs#L24-L53):

```csharp
if (existingToken != null)
{
    _context.RefreshTokens.Remove(existingToken);
    await _context.SaveChangesAsync();   // ← SaveChanges #1 (L34)
}
// ...
_context.RefreshTokens.Add(refreshTokenEntity);
await _context.SaveChangesAsync();       // ← SaveChanges #2 (L52)
```

Son dos `SaveChangesAsync` en llamadas separadas, **sin** `BeginTransaction`/`TransactionScope` envolvente. La clase está marcada `[ExcludeFromCodeCoverage]` (L11) y **no tiene test dedicado** ([06](06-tests.md)).

**Riesgo real: Alto (robustez).** Si el proceso cae o el segundo `SaveChangesAsync` falla (timeout Oracle, violación de constraint, pérdida de conexión) **después** de que el primero borró el token anterior, la persona queda **sin ningún refresh token activo**: el viejo ya se eliminó físicamente y el nuevo nunca se persistió. Consecuencia funcional: el próximo `RefreshToken` falla → se limpian cookies → el usuario es deslogueado y debe reautenticarse. No hay pérdida de datos críticos ni corrupción, pero sí una ventana de inconsistencia. Además, al ser `Remove` físico (no soft-delete), no queda historial para detectar reuso de token robado.

**Cambio mínimo recomendado:** eliminar el primer `SaveChangesAsync` y persistir `Remove` + `Add` en una **sola** llamada, de modo que EF los agrupe en una transacción implícita:

```csharp
if (existingToken != null)
    _context.RefreshTokens.Remove(existingToken);
_context.RefreshTokens.Add(refreshTokenEntity);
await _context.SaveChangesAsync();   // único commit atómico
```

> ⚠️ Verificar que no exista una PK/constraint única sobre `(CodigoPersona, Sistema)` que rechace tener el registro viejo y el nuevo en el mismo `SaveChanges`. Si la hay, envolver en `IDbContextTransaction` explícita en lugar de fusionar. **Requiere confirmación** del esquema de la tabla `RefreshTokens`.

**Tests que deberían existir antes de tocarlo:**
1. Guarda token nuevo cuando no existe uno previo → queda 1 activo.
2. Guarda token nuevo cuando ya existe uno → el viejo se reemplaza, queda exactamente 1.
3. **Si el `Add`/segundo commit falla, el token anterior NO debe haberse borrado** (este test falla hoy y pasa tras el fix — demuestra el bug).
4. `IsActive`, `ExpiresAt`, `CreatedAt` correctos en el registro resultante.

---

## 2. Devolución de `ex.Message` al cliente

**Veredicto: ✅ Confirmado (y más serio de lo que parecía).**

Existe un mecanismo central correcto de redacción — `ExceptionHandlingMiddleware` **oculta el mensaje en producción** ([ExceptionHandlingMiddleware.cs:127-133](../../WebApiAdmisiones/WebApiAdmisiones/Security/Middleware/ExceptionHandlingMiddleware.cs#L127-L133)):

```csharp
var result = OperationResult<string>.IsFailed(
    errorCode, ..., "Error inesperado",
    StatusCodes.Status500InternalServerError,
    !_env.IsProductionLike() ? exceptionMessage : null);   // ← redacta en prod
```

**El problema:** los servicios **no lanzan** la excepción — la capturan y devuelven `ex.Message` dentro de `OperationResult.Message`, que se serializa como respuesta normal. Eso **saltea el middleware por completo** y **filtra el detalle en TODOS los ambientes, incluida producción.**

**Evidencia exacta — fugas reales a cliente (bypass del middleware):**

| Archivo:línea | Método | Mensaje |
|---------------|--------|---------|
| [AuthService.cs:161](../../WebApiAdmisiones/AppLogic/Services/Autenticacion/AuthService.cs#L161) | `AutenticarUsuarioLDAPAsync` | `$"Error al autenticar usuario: {ex.Message}"` |
| [AuthService.cs:256](../../WebApiAdmisiones/AppLogic/Services/Autenticacion/AuthService.cs#L256) | `RefrescarTokensAsync` | `$"Error al refrescar tokens: {ex.Message}"` |
| [AuthService.cs:453](../../WebApiAdmisiones/AppLogic/Services/Autenticacion/AuthService.cs#L453) | `CompletarPasswordAsync` | `$"Error al completar password inicial: {ex.Message}"` |
| [AuthService.cs:663](../../WebApiAdmisiones/AppLogic/Services/Autenticacion/AuthService.cs#L663) | `GenerarTokensParaPersonaAsync` | `$"Error al generar tokens: {ex.Message}"` |
| [RegistroService.cs:427](../../WebApiAdmisiones/AppLogic/Services/Registro/RegistroService.cs#L427) | `CompletarNuevaPersonaAsync` | `$"Error al crear la persona: {ex.Message}"` |
| [RegistroService.cs:518](../../WebApiAdmisiones/AppLogic/Services/Registro/RegistroService.cs#L518) | `ConfirmarNuevaPersonaAsync` | `$"Error al crear la persona: {ex.Message}"` |
| [RegistroService.cs:552](../../WebApiAdmisiones/AppLogic/Services/Registro/RegistroService.cs#L552) | (registrar admisión) | `$"Error al registrar la admisión: {ex.Message}"` |
| [RegistroService.cs:585](../../WebApiAdmisiones/AppLogic/Services/Registro/RegistroService.cs#L585) | `ConfirmarSolicitudAltaAsync` | `$"Error al crear la solicitud de alta: {ex.Message}"` |
| [PersonaService.cs:181](../../WebApiAdmisiones/AppLogic/Services/Personas/PersonaService.cs#L181) | `CambiarPasswordAsync` | `$"Error al cambiar contraseña: {ex.Message}"` |
| [PasswordActivationService.cs:143](../../WebApiAdmisiones/AppLogic/Services/Autenticacion/PasswordActivationService.cs#L143) | `EnviarMailNuevaPersonaAsync` | `$"...: {ex.Message}"` |
| [PasswordActivationService.cs:214](../../WebApiAdmisiones/AppLogic/Services/Autenticacion/PasswordActivationService.cs#L214) | `EnviarMailLink/Recuperacion` | `$"Error al enviar link de {flow.Descripcion}: {ex.Message}"` |
| [PasswordActivationService.cs:289](../../WebApiAdmisiones/AppLogic/Services/Autenticacion/PasswordActivationService.cs#L289) | `ActivarLinkPasswordAsync` | `$"Error al activar link de contraseña: {ex.Message}"` |
| [PasswordActivationService.cs:479](../../WebApiAdmisiones/AppLogic/Services/Autenticacion/PasswordActivationService.cs#L479) | `ValidarSessionToken` | `$"Error al validar sesión temporal: {ex.Message}"` |
| [RecaptchaService.cs:112](../../WebApiAdmisiones/WebApiAdmisiones/Security/Captcha/RecaptchaService.cs#L112) | `ValidarConScoreAsync` | `$"Error al validar captcha: {ex.Message}"` |
| [InscripcionesyPagosApiClient.cs:591](../../WebApiAdmisiones/AppLogic/ApiClients/InscripcionesyPagosApiClient.cs#L591), [:595](../../WebApiAdmisiones/AppLogic/ApiClients/InscripcionesyPagosApiClient.cs#L595) | `HandleException` | `$"Error de red: {ex.Message}"`, `$"Error: {ex.Message}"` (detalle de la API interna) |

**Casos que NO son fuga (falsos positivos a excluir):**
- [ExceptionHandlingMiddleware.cs:82,142](../../WebApiAdmisiones/WebApiAdmisiones/Security/Middleware/ExceptionHandlingMiddleware.cs#L82) — es el handler central; **redacta en prod** (L132). Correcto, no tocar.
- [SanitizeAttribute.cs:62](../../WebApiAdmisiones/WebApiAdmisiones/Security/RequestValidation/SanitizeAttribute.cs#L62) — la `InputSanitizationException` termina en el mismo middleware, que **redacta en prod**. Aceptable.
- Tests (`SanitizeAttributeTests.cs:123`) — no aplica.

**Caso menor (revisar, baja severidad):**
- [JsonSchemaValidationFilter.cs:155](../../WebApiAdmisiones/WebApiAdmisiones/Security/RequestValidation/JsonSchemaValidationFilter.cs#L155) — devuelve `new { jex.Message }` (error de parseo JSON) en 400, **sin** redacción y en todos los ambientes. Los mensajes de parser JSON rara vez son sensibles (describen el JSON malformado del propio cliente) y son útiles para el front. Severidad baja; documentar la decisión.

**Riesgo real: Alto (seguridad — information disclosure).** Los mensajes pueden exponer detalle de infraestructura (excepciones de Oracle/EF, errores LDAP, rutas, tipos internos) a un cliente no autenticado en varios de estos endpoints (`Login`, `RefreshToken`, `EvaluarDocumento`, `RecuperarContraseña`, captcha). Además, es una **inconsistencia**: el proyecto ya invirtió en un mecanismo de redacción que estos catch anulan.

**Cambio mínimo recomendado:** un helper compartido que loguee la excepción real (con `correlationId`) y devuelva un mensaje genérico + código de error, sin `ex.Message`. Reemplazar los `$"...: {ex.Message}"` por el mensaje genérico. El `ex` sigue logueándose server-side para diagnóstico.

```csharp
// en cada catch:
_logger?.LogError(ex, "Fallo en {Metodo}", nameof(...));
return OperationResult<T>.IsFailed("CODE_99", nameof(...), "No se pudo completar la operación.", 500, default!);
```

**Tests que deberían existir antes de tocarlo:**
1. Por cada método afectado: cuando el colaborador lanza excepción, el `OperationResult.Message` devuelto **no contiene** el texto de `ex.Message` (assert negativo).
2. Que la excepción **sí** se loguee (verify sobre `ILogger`).
3. Que el `ErrorCode` y `HttpCode` se conserven.

---

## 3. `new EncuestaInicialService(...)` dentro de `InscripcionesService`

**Veredicto: ✅ Confirmado.**

**Evidencia exacta** — [InscripcionesService.cs:386-406](../../WebApiAdmisiones/AppLogic/Services/Inscripciones/InscripcionesService.cs#L386-L406). Se instancia manualmente en **dos** métodos:

```csharp
public OperationResult<...> ObtenerEncuestaInicial(long codigoPersona)
{
    var encuestaInicialService = new EncuestaInicialService(   // L388
        _uowFactory, _dbConnectionContext, _generalService, _tivenosEnvioService);
    return encuestaInicialService.ObtenerEncuestaInicial(codigoPersona);
}

public OperationResult<...> GuardarEncuestaInicial(long codigoPersona, ...)
{
    var encuestaInicialService = new EncuestaInicialService(   // L399
        _uowFactory, _dbConnectionContext, _generalService, _tivenosEnvioService);
    return encuestaInicialService.GuardarEncuestaInicial(codigoPersona, request);
}
```

`EncuestaInicialService` es `internal sealed` **sin interfaz** ([02](02-matriz-services.md)), por eso no puede inyectarse ni mockearse; se crea a mano en cada request.

**Riesgo real: Medio (testeabilidad/mantenibilidad), no de runtime.** No es un bug: como todas las dependencias que recibe son las mismas que `InscripcionesService` ya tiene inyectadas, la instancia se arma bien y comparte el mismo `IUnitOfWorkFactory`. El costo es: (a) no se puede mockear `EncuestaInicialService` en los tests de `InscripcionesService` (por eso su lógica solo se cubre indirectamente — ver brecha alta en [06](06-tests.md)); (b) rompe la convención DI del resto del proyecto; (c) se crea un objeto nuevo por llamada (irrelevante en performance).

**Cambio mínimo recomendado:**
1. Extraer `IEncuestaInicialService` con los dos métodos públicos.
2. Registrarlo como `Scoped` en `DomainServicesExtensions`.
3. Inyectarlo en `InscripcionesService` y borrar los dos `new`.

**Tests que deberían existir antes de tocarlo:**
1. **Primero, tests dedicados de `EncuestaInicialService`** (hoy no existen — brecha alta): `ObtenerEncuestaInicial` y `GuardarEncuestaInicial` (completitud temporal/definitiva, sincronización de bachillerato). Sin ellos el refactor es a ciegas.
2. Tras extraer la interfaz: test de `InscripcionesService` que verifique que delega en el mock de `IEncuestaInicialService` (posible ahora, imposible hoy).

---

## 4. Uso de `IServiceScopeFactory` en `AuthService` y `RegistroService`

**Veredicto: ⚠️ Parcialmente confirmado** (es real, pero es un *workaround* deliberado, no un bug evidente).

**Evidencia exacta:**

- `AuthService`: dependencia opcional `IServiceScopeFactory? _serviceScopeFactory` ([AuthService.cs:31,50](../../WebApiAdmisiones/AppLogic/Services/Autenticacion/AuthService.cs#L31)). Uso en [AuthService.cs:591-604](../../WebApiAdmisiones/AppLogic/Services/Autenticacion/AuthService.cs#L591-L604): si es null usa el factory inyectado; si no, **crea un scope nuevo** y resuelve `IUnitOfWorkFactory` + `IRefreshTokenService` desde él.
- `RegistroService`: mismo patrón en los tres wrappers LDAP — [RegistroService.cs:669-710](../../WebApiAdmisiones/AppLogic/Services/Registro/RegistroService.cs#L669-L710) (`ExisteUsuarioLdapAsync`, `CrearUsuarioLdapAsync`, `CambiarPasswordLdapAsync`): si es null usa `_ldap`; si no, crea scope y resuelve `ILdap` desde él.

**Matiz importante:** aunque el parámetro es opcional con default `null`, **`IServiceScopeFactory` siempre está registrado por defecto en el contenedor de ASP.NET Core**. Por lo tanto en producción el parámetro **se inyecta** y la ruta activa es la de "crear scope anidado". El `= null` solo aplica cuando se construye el servicio a mano en tests.

**Riesgo real: Medio.** No hay evidencia de fallo funcional, pero:
- Es un patrón **service-locator** que oscurece las dependencias reales y complica el razonamiento sobre lifetimes.
- Crear un scope anidado por operación levanta un `DbContext`/UoW (y una conexión `ILdap`) nuevos dentro del mismo request. Esto sugiere que el diseño necesita "aislar" ciertas operaciones del `DbContext` ambiente — típicamente un workaround para: (a) operar tras un commit/rollback que dejó el contexto del request en estado sucio, o (b) evitar problemas de concurrencia/estado en `ILdap`.
- En `AuthService` se usa justo en `GenerarTokensParaPersonaAsync`, invocado tras crear una persona nueva en el flujo de registro diferido — coherente con "necesito un contexto fresco después de una escritura previa".

**Cambio mínimo recomendado:** **No cambiar todavía — investigar primero.** Determinar por qué se introdujo el scope anidado (revisar historial/PR). Si se confirma que es innecesario (las operaciones pueden compartir el scope del request), reemplazar por inyección directa de `IUnitOfWorkFactory`/`IRefreshTokenService`/`ILdap` y eliminar `IServiceScopeFactory`. Si es un workaround legítimo (p. ej. contexto sucio tras rollback), **documentarlo con un comentario** explicando la razón, en lugar de dejarlo implícito. Es el ítem de mayor riesgo del plan (5.8 en [07](07-plan-refactor.md)).

**Tests que deberían existir antes de tocarlo:**
1. `GenerarTokensParaPersonaAsync` con y sin `IServiceScopeFactory` provisto → mismo resultado observable (ya hay cobertura parcial en `AuthServiceTests`).
2. Flujo de registro diferido de punta a punta (crear persona → generar tokens) que hoy ejercita la ruta de scope anidado, para no regresionar al eliminarla.
3. Los tres wrappers LDAP de `RegistroService` con y sin scope factory.

---

## 5. Estado real de `FondoDeBecaController` y sus tests

**Veredicto: ✅ Confirmado — y corrige un error de la auditoría previa.**

**Evidencia exacta:**
- **Controller:** los ~12 endpoints están **todos comentados** — [FondoDeBecaController.cs:30-293](../../WebApiAdmisiones/WebApiAdmisiones/Controllers/FondoDeBecaController.cs#L30-L293). La clase existe, inyecta `IFondoDeBecaServices`, pero **no expone ninguna ruta HTTP**.
- **Tests del controller:** **también están comentados en su totalidad.** El bloque `/*` abre en `FondoDeBecaControllerTests.cs:17` y cierra `*/` en la línea 448 (de 450 líneas totales). → **0 tests activos** para el controller.
  - ⚠️ **Corrección a [06](06-tests.md)/[01](01-matriz-controllers.md):** allí se reportó "✅ ~21 tests" para `FondoDeBecaController`. Es un **falso positivo**: esos tests no compilan como tests activos porque están dentro de un comentario de bloque. Debe leerse **0 tests activos**.
- **Tests del servicio:** `FondoDeBecaServiceTests.cs` **sí tiene 37 `[Fact]`/`[Theory]` activos** (verificado: no están comentados). El servicio está probado a nivel unitario aunque no tenga consumidor HTTP.

**Riesgo real: Medio (código durmiente / mantenimiento).** No es un riesgo de seguridad ni de runtime (no hay rutas expuestas). Los riesgos son:
- **Confusión y falso sentido de cobertura:** el controller y sus tests aparentan existir pero no ejercen nada. La auditoría inicial cayó en esto.
- **Deriva:** `FondoDeBecaService` (13 métodos, 37 tests que se mantienen y ejecutan) evoluciona sin consumidor, gastando esfuerzo de CI en código sin ruta.
- **Deuda de decisión:** hay una feature completa (declaración jurada + adjuntos) construida, testeada y desactivada, sin nota de por qué.

**Cambio mínimo recomendado:** decisión de negocio explícita entre tres opciones, y dejarla registrada:
1. **Activar** — descomentar controller + tests si la feature va a producción pronto (validar rutas, auth y captcha antes).
2. **Eliminar** — borrar controller, tests comentados, `FondoDeBecaService`, `IFondoDeBecaServices`, su registro DI y `CatalogosService.ObtenerFondosDeBecaPorProducto`, si la feature se descartó.
3. **Documentar** — si queda en espera, agregar un comentario/README indicando el motivo y la fecha objetivo, y **decidir si los 37 tests del servicio deben seguir corriendo** mientras tanto.

> **Requiere confirmación del equipo de producto/negocio.** Por la regla de la auditoría, ningún método público de servicio detrás de endpoint comentado se elimina automáticamente (puede reactivarse).

**Tests que deberían existir antes de tocarlo:**
- Si se **activa**: descomentar y hacer compilar los tests del controller; agregar tests de integración de las rutas (auth + captcha + validación de archivos).
- Si se **elimina**: no se requieren tests nuevos; verificar que la solución compila y que ningún otro consumidor (incluido `CatalogosService`) queda colgado.

---

## Resumen de veredictos

| # | Hallazgo | Veredicto | Riesgo real | Requiere test antes |
|---|----------|-----------|:---:|:---:|
| 1 | `SaveRefreshTokenAsync` sin transacción | ✅ Confirmado | Alto (robustez) | Sí (crítico) |
| 2 | `ex.Message` al cliente | ✅ Confirmado (bypassa la redacción existente → filtra en prod) | Alto (seguridad) | Sí |
| 3 | `new EncuestaInicialService` | ✅ Confirmado | Medio (testeabilidad) | Sí (tests de EncuestaInicialService primero) |
| 4 | `IServiceScopeFactory` (service-locator) | ⚠️ Parcial (real, pero workaround deliberado) | Medio | Sí — investigar antes de cambiar |
| 5 | `FondoDeBecaController` desactivado | ✅ Confirmado + **corrige "21 tests" → 0 tests activos** del controller (el servicio sí tiene 37) | Medio (código durmiente) | Depende de la decisión |

**Ningún hallazgo resultó falso positivo.** El único ajuste es el conteo de tests del punto 5 (los del *controller* están comentados; los del *servicio* corren). Correcciones a propagar en [01](01-matriz-controllers.md) y [06](06-tests.md).
