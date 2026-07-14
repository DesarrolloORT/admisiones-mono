# AGENTS.md — api-admisiones

Instrucciones para cualquier agente (o humano) que implemente features, bugs o refactors en este repositorio. Origen: auditoría `docs/AUDITORIA-CALIDAD-CODIGO.md` (2026-07-14) — ahí está la evidencia detrás de cada regla.

> Nota sobre `.github/copilot-instructions.md`: es el baseline compartido del equipo, pero su contenido actual describe otro proyecto (API Ficha de Persona: rutas `FichaDePersona/`, `WebApiFDP.sln`, .NET 9). Los **patrones** que describe (OperationResult, UoW, Devart, tests) sí aplican; los **paths y comandos** no. Ante conflicto, manda este archivo.

## Prioridades

1. Resuelve con el menor contexto y la menor salida útil posible.
2. Sigue patrones existentes del repo antes de introducir abstracciones.
3. Trata seguridad como restricción de generación.
4. Carga instrucciones, prompts o skills solo cuando aporten contexto real.

## Contexto del proyecto

- **Qué es:** API REST de admisiones de ORT Uruguay. Autenticación (LDAP + JWT en cookie HttpOnly + 2FA adaptativo por score de reCAPTCHA), registro/onboarding con reconocimiento de documentos (Azure AI), catálogos, personas, inscripciones y pagos (vía API interna), encuesta inicial, becas.
- **Stack:** .NET 10 · ASP.NET Core Web API · EF Core con proveedor **Devart para Oracle** (código generado por Entity Developer) · Redis (rate limiting, sesiones 2FA, tokens de activación, cache de catálogos) · Serilog + OpenTelemetry + Prometheus · xUnit + Moq.
- **Restricción dura:** `Core/` es un **submódulo privado** imprescindible para el build. Todo lo que está bajo `DevartDTOs/`, `DevartEFCore/`, `DevartDataAccess/` es **generado** — no se edita a mano (se pierde al regenerar); los repos se extienden con clases `partial` hand-written en el namespace correcto.
- **Consumidor:** un front Angular. No hay versionado de API: cualquier cambio de ruta, shape de response o formato de error es breaking change y hay que avisar.

### Estructura real (post refactor module-first, PR #107)

```
api-admisiones/
├── Core/                          # Submódulo privado compartido entre proyectos ORT
│   ├── Utilities/                 # OperationResult<T>, Encriptador, constantes
│   ├── DbConnectionContext/       # Conexión/transacción Oracle compartida
│   ├── LdapService/  MailORT/  AzureService/  Modules/
└── WebApiAdmisiones/
    ├── WebApiAdmisiones/          # Entrypoint: Program.cs, Controllers/ (6 + ApiBaseController),
    │                              # Extensions/ (composition root), Security/
    ├── AppLogic/                  # POR FEATURE: Autenticacion/ Registro/ Inscripciones/ (+Encuesta/)
    │                              # Personas/ Becas/ Catalogos/ Tivenos/ ApiClients/ Common/ Infrastructure/
    │                              # Cada feature: Interfaces/ Services/ Dtos|Requests|Responses/ (+ Validators/,
    │                              # Rules/, Constants/, Mappers/, Helpers/ solo si el módulo los llena)
    │                              # DevartDTOs/ = GENERADO
    ├── BusinessLogic/             # Entidades + interfaces de repos (mayoría GENERADO Devart)
    ├── DataAccess/                # Repos Devart (mayoría GENERADO; ~40 partial hand-written) + RefreshTokenService
    └── UnitTesting/               # xUnit + Moq, ~935 casos
```

Controllers reales: `AuthController`, `RegistroController`, `CatalogosController`, `InscripcionesController`, `PersonaController`, `BecasController`. (No existen `LoginController`, `PreinscripcionController` ni `FondoDeBecaController`, aunque documentación vieja los mencione.)

## Proceso obligatorio antes de modificar código

1. Leer este archivo y, si el cambio toca un área auditada, la sección correspondiente de `docs/AUDITORIA-CALIDAD-CODIGO.md`.
2. Identificar el módulo responsable en `AppLogic/<Feature>/` y si el archivo es generado o hand-written (header "Devart Entity Developer" = generado, no tocar).
3. Revisar una implementación similar existente en el mismo módulo y copiar su patrón (service → `OperationResult<T>` → `ValidateResponse`).
4. Buscar TODAS las referencias del código afectado (incluye tests y `DomainServicesExtensions.cs`) antes de cambiar firmas o borrar.
5. Confirmar contratos públicos que no deben romperse: rutas HTTP, shape JSON de requests/responses, códigos de error `ÁREA_MET_NN`, claves de configuración y variables de entorno.
6. Ubicar los tests existentes del área (`UnitTesting/` espeja la estructura, con excepciones históricas: `Services/AuthServiceTests.cs` en la raíz, `LoginFlowServiceTests` en `Security/`).
7. Para cambios grandes (nuevo flujo, cambio transaccional, cambio de contrato): explicar el enfoque en 3-5 líneas antes de implementar.

## Reglas para nuevas features

**Organización**
- La feature vive en `AppLogic/<Feature>/` con `Interfaces/`, `Services/`, `Dtos|Requests|Responses/`. No inventar carpetas por plantilla: `Validators/`, `Rules/`, `Constants/`, `Mappers/` solo si se llenan.
- Un módulo no llama a otro por métodos estáticos ni `internal` backdoors: siempre por la interfaz registrada en DI.

**Controllers**
- El controller valida el borde, llama a UN service y devuelve `ValidateResponse(resultado)`. Orquestación de dos services o mapeo de DTOs → baja al service.
- `[Authorize]` a nivel clase; `[AllowAnonymous]` explícito por action con comentario del porqué. La identidad SIEMPRE sale del token (`_currentUser.GetUserId()`), nunca de un parámetro del cliente.
- `[ProducesResponseType]` por cada código realmente devuelto; XML doc del endpoint (Swagger lo publica — que no mienta).
- Ningún tipo `*Devart` en firmas de controller: el contrato público usa DTOs propios del módulo.
- Ningún dato hardcodeado/mock en endpoints productivos.

**Services y errores**
- Todo método de service devuelve `OperationResult<T>`: `Ok(dto, nameof(Metodo))` / `IsFailed(errorCode, nameof(Metodo), mensaje, httpCode)`. Código de error único con prefijo del área (mirar los existentes del módulo antes de inventar uno).
- Éxito parcial no existe: si un paso secundario falla (mail, cache), va en un campo estructurado del response + `LogWarning` — nunca solo en el texto del mensaje.
- Estados, tipos de documento y steps se comparan contra constantes nombradas (`InscripcionesConstants`, `DocumentUtils`), nunca literales `"CI"`/`"Confirmada"`.
- Catches específicos; un `catch (Exception)` solo con `LogError` + rollback/rethrow o conversión a `OperationResult` con código `_99`.

**DTOs y validaciones**
- Requests/Responses en el módulo; propiedades sensibles (passwords, tokens, documentos) con `[Redact]` — el `InputRedactionLoggingFilter` loguea todo lo no marcado.
- Ojo con el doble contrato de error: los `[Required]` disparan el 400 `ProblemDetails` automático de `[ApiController]`, distinto al envelope `OperationResult` (hallazgo CTL-03, pendiente de unificar). Hasta que se unifique, seguir el patrón del módulo que tocás.

**Transacciones y efectos externos**
- Una transacción por método de service: `uow.BeginTransaction()` → trabajo DB → `Commit()`, con `catch { Rollback(); throw; }`. **Nunca anidar** transacciones ni seguir usando el contexto después de un `Rollback` (el UoW comparte un `ModelContext` scoped: no aísla — hallazgo DAT-01).
- Ningún `await` a un sistema externo (LDAP, HTTP, mail, Redis) dentro de una transacción DB abierta.
- Efectos externos irreversibles (crear usuario LDAP, consumir un token) van DESPUÉS de persistir, o con compensación y `LogError` explícito del estado parcial.
- `using var uow = _uowFactory.Create();` siempre.

**Acceso a datos**
- Los filtros van en SQL: agregar un método específico en el repo `partial` hand-written (patrón `GetInscripcionFrescoHabilitada`). `GetAll()` solo para catálogos chicos.
- Repos `partial` nuevos: verificar el namespace contra el archivo generado (hubo stubs muertos por namespace equivocado); lecturas puras con `AsNoTracking()`; métodos nuevos nacen async con `CancellationToken` cuando el template lo permita — si el repo circundante es sync, seguir sync y no fingir async.
- No reemplazar automáticamente `Count() > 0` por `Any()`. Con el proveedor Devart para Oracle utilizado en este proyecto, ciertas consultas con `Any()` pueden no traducirse correctamente o fallar en ejecución.
- Antes de cambiar una expresión de existencia, revisar el patrón utilizado por los repositorios actuales y validar la consulta generada contra Oracle.
- Mantener `Count() > 0` cuando sea el patrón compatible y probado del repositorio.
- `Any()` puede utilizarse únicamente cuando se haya comprobado que el proveedor lo traduce correctamente para esa consulta concreta o cuando se opere sobre una colección ya materializada en memoria.
- Usar `AsSplitQuery()` si hay `Include` de 2+ colecciones; no aplicar funciones (`Trim()`, `ToUpper()`) sobre columnas en predicados sin confirmar índice funcional.

**Async y CancellationToken**
- Sin `.Result`, `.Wait()`, `Task.Run` para fingir async, ni `async void`. Sin `async` en métodos sin `await` (usar `Task.FromResult`).
- Servicios nuevos que hagan I/O real async: aceptar y propagar `CancellationToken` desde el controller (`HttpContext.RequestAborted`). No agregar CT a firmas existentes de forma masiva.

**Dependency Injection**
- Todo registro va en el extension method correspondiente de `Extensions/` (dominio → `DomainServicesExtensions`, lifetime scoped por defecto). `Program.cs` no se infla.
- Todas las dependencias de constructor son **requeridas**: prohibido `ILogger<T>? = null`, dependencias funcionales opcionales y segundos constructores públicos.
- Sin service locator (`GetRequiredService`) fuera de la composition root — el patrón `serviceScopeFactory` existente en `AuthService`/`RegistroService` es deuda conocida en espera: no replicarlo.

**Configuración y secretos**
- Secretos SOLO por variable de entorno con fail-fast al arranque (patrón `OracleConnectionStringAdmisiones` / `RedisConnectionStringAdmisiones` / `JWT_SECRET_KEY`). Jamás en `appsettings*.json`.
- Una clave de configuración = un único punto de lectura. Config nueva no-secreta: `IOptions<T>` con `ValidateOnStart`.
- El orden del pipeline en `MiddlewarePipelineExtensions.cs` es crítico: no reordenar ni quitar filtros globales (`SanitizeAttribute`, `InputRedactionLoggingFilter`, `JsonSchemaValidationFilter`) sin justificación de seguridad.

**Logging**
- Serilog vía `ILogger<T>` (nunca `Console.WriteLine`). Nada de PII/credenciales/tokens en logs; el correlation id ya fluye — no crear identificadores paralelos.

**Tests**
- Toda regla de negocio nueva llega con tests de caminos de error, no solo happy path (estándar de referencia: `AuthServiceTests`).
- Naming `Metodo_Escenario_ResultadoEsperado`, patrón AAA. Asserts sobre `ErrorCode`/`HttpCode` (estables), no sobre el texto completo del mensaje.
- Tests que tocan env vars: `EnvironmentVariableScope` + `[Collection(EnvironmentVariablesCollection.Name)]`, sin excepciones.
- Código que toca Redis/cookies/seguridad: testear el contrato real (TTL, atributos de cookie, ventanas), no solo el consumidor con el store mockeado.

**Documentación y compatibilidad**
- Cambio de ruta, shape de response, formato de error o variable de entorno = breaking change: documentarlo en el PR y avisar al front. Actualizar `README.md` si cambian env vars o endpoints.

## Reglas para refactors

- No mezclar refactor amplio con cambio funcional en el mismo PR salvo necesidad justificada.
- Preservar comportamiento y contratos: los tests existentes deben pasar sin cambiar sus asserts (solo setup si cambió el wiring).
- Agregar tests ANTES de cambiar código crítico sin cobertura.
- Buscar todas las referencias antes de eliminar métodos (incluye tests, DI y usos por reflexión en tests de atributos).
- No crear abstracciones especulativas ni interfaces sin necesidad concreta (una interfaz nueva se justifica con: segundo implementador real, necesidad de mock, o boundary de módulo).
- No mover código entre módulos sin revisar dependencias y namespaces (`git mv` + using, sin tocar lógica — método validado en el piloto Tivenos, `docs/11-estructura-tivenos-piloto.md`).
- Cambios chicos y revisables; para estructura, un módulo por rama.
- Zona LogicaORT legacy (fórmulas de encuesta, mapeos numéricos heredados): nombrar constantes sí, cambiar fórmulas/config NO sin preguntar.
- No "arreglar" la convención `DateTime.Now` (negocio/Oracle SYSDATE) vs `UtcNow` (tokens): es la convención del esquema legacy, está investigada y cerrada.

## Checklist de calidad (antes de dar por terminada una tarea)

- [ ] Compila sin warnings nuevos (`dotnet build`).
- [ ] Toda la suite de tests pasa; el área tocada tiene tests de sus caminos de error.
- [ ] Sin secretos, PII ni datos sensibles en código, config commiteada o logs; DTOs sensibles con `[Redact]`.
- [ ] Sin literales mágicos nuevos de estado/tipo de documento; códigos de error únicos y con prefijo del área.
- [ ] Sin efectos externos dentro de transacciones; transacciones con rollback en catch.
- [ ] Contratos públicos intactos (o breaking change documentado y avisado).
- [ ] Registro DI agregado en el extension method correcto; el grafo resuelve (`DomainServicesExtensionsTests` verde).
- [ ] **Sin archivos Devart regenerados en el commit**: `dotnet build` regenera ~280 archivos generados con timestamp nuevo — hacer `git checkout` de los no tocados intencionalmente antes de commitear.
- [ ] Si se tocó `Core/`: commit + push del submódulo ANTES del puntero en este repo, y verificado con `git submodule status` (hubo un fix de seguridad "hecho" que nunca llegó al árbol por saltarse esto).
- [ ] Documentación afectada actualizada (README, XML docs de endpoints).

## Validación obligatoria (comandos reales del repo)

```bash
# Submódulo (imprescindible; MSB3202 = submódulo sin inicializar)
git submodule update --init --recursive

# Restaurar y compilar (desde la raíz del repo)
dotnet restore WebApiAdmisiones/WebApiAdmisiones.sln
dotnet build WebApiAdmisiones/WebApiAdmisiones.sln -c Debug

# Tests
dotnet test WebApiAdmisiones/UnitTesting/UnitTesting.csproj

# Tests con cobertura (reportes en WebApiAdmisiones/UnitTesting/TestResults/)
dotnet test WebApiAdmisiones/UnitTesting/UnitTesting.csproj --settings WebApiAdmisiones/UnitTesting/tests.runsettings --collect:"XPlat Code Coverage"

# Ejecutar localmente (requiere env vars: OracleConnectionStringAdmisiones, JWT_*, RedisConnectionStringAdmisiones)
dotnet run --project WebApiAdmisiones/WebApiAdmisiones/WebApiAdmisiones.csproj --launch-profile http
```

No hay `.editorconfig`, analizadores adicionales ni `dotnet format` configurados en el repo: no inventar pasos de formato; el análisis estático corre en CI vía SonarQube (`.github/workflows/ci-sonarqube-tests.yml`, proyecto `ApiAdmisiones` en el SonarQube self-hosted del equipo).

## Formato de entrega al finalizar una tarea

Informar:

1. **Qué cambió** y **por qué** (2-4 líneas).
2. **Archivos modificados** (lista).
3. **Decisiones importantes** tomadas y alternativas descartadas.
4. **Tests** agregados o actualizados.
5. **Validaciones ejecutadas** (build/tests, con resultado real — si algo falla, decirlo con el output).
6. **Riesgos o pendientes** que quedan abiertos.
7. **Posibles cambios de contrato** (rutas, shapes, códigos de error, env vars) — explícitamente "ninguno" si no los hay.

## Perfiles (baseline del equipo)

- common: baseline común y CodeGraph (este archivo).
- back: reglas .NET backend (`.github/instructions/toolkit/dotnet-backend.instructions.md` — vigente y consistente con este repo).
- secure-code: `.github/instructions/toolkit/secure-code.instructions.md` — vigente.
