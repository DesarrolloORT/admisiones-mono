# Guía de estilo de código — api-admisiones

Cómo escribir código **legible y uniforme** en esta API. Es la referencia para personas;
`AGENTS.md` sigue siendo la fuente canónica de reglas de arquitectura/seguridad y manda ante
conflicto. Lo mecánico (namespaces, prefijos, formato) lo aplica el `.editorconfig` de la raíz;
esta guía cubre lo que ninguna herramienta puede decidir por vos: **cómo se lee el código**.

El norte es simple: un endpoint se tiene que poder leer de arriba a abajo, una vez, y entender.
La referencia de "así se ve bien" es
[`FondoDeBecaService.cs`](../WebApiAdmisiones/AppLogic/Becas/Services/FondoDeBecaService.cs).

---

## 1. Propagación de errores: nunca re-copiar campos a mano

Todo devuelve `OperationResult<T>`. Cuando un método llama a un helper que ya devuelve un
`OperationResult` y hay que cortar ante el fallo, **no rearmes el error campo por campo**.

- Si el tipo de dato **no cambia**, devolvé el resultado tal cual: `return resultado;`
- Si el tipo **cambia**, reproyectá el error con el helper
  [`OperationResultExtensions`](../WebApiAdmisiones/AppLogic/Helpers/OperationResultExtensions.cs):
  `return resultado.Failure().As<TipoDestino>();`

```csharp
// ❌ Antes: 6 líneas de ruido que sepultan la lógica real
var ingresoResult = ObtenerIngresoAutorizado(uow, codigoPersona, id, nameof(SubirArchivoIngreso), "FDB_SAI");
if (!ingresoResult.Success || ingresoResult.Data is null)
{
    return OperationResult<bool>.IsFailed(
        ingresoResult.ErrorCode,
        nameof(SubirArchivoIngreso),
        ingresoResult.Message,
        ingresoResult.HttpCode);
}

// ✅ Después: una línea; se lee la intención
var ingresoResult = ObtenerIngresoAutorizado(uow, codigoPersona, id, nameof(SubirArchivoIngreso), "FDB_SAI");
if (!ingresoResult.Success || ingresoResult.Data is null)
    return ingresoResult.Failure().As<bool>();
```

`Failure().As<T>()` preserva `ErrorCode`, `Method`, `Message` y `HttpCode` exactos: no cambia
el contrato con el front, sólo borra el boilerplate.

Si propagás desde un validador/regla cuyo nombre **no** querés que aparezca en la respuesta,
re-sellá el método de origen: `return validacion.Failure().As<TipoDestino>(methodName);`

---

## 2. Métodos de servicio: guard clauses y un solo nivel de indentación

Un método = una responsabilidad. El **camino feliz** va sin anidar; cada precondición sale
temprano (`return`). Nada de `if/else` anidado profundo.

```csharp
// ✅ Patrón estándar de un método de servicio
public OperationResult<bool> SubirArchivoIngreso(long codigoPersona, long id, byte[] fileContent, string fileName)
{
    var archivoValidado = FondoDeBecaValidation.ValidarArchivoAdjunto(fileContent, fileName, nameof(SubirArchivoIngreso));
    if (!archivoValidado.Success)
        return archivoValidado.Failure().As<bool>();

    using var uow = _uowFactory.Create();
    var ingresoResult = ObtenerIngresoAutorizado(uow, codigoPersona, id, nameof(SubirArchivoIngreso), "FDB_SAI");
    if (!ingresoResult.Success || ingresoResult.Data is null)
        return ingresoResult.Failure().As<bool>();

    var ingreso = ingresoResult.Data;
    ingreso.ArchivoIngresoNfDj = fileContent;
    uow.Save();

    return OperationResult<bool>.Ok(true, nameof(SubirArchivoIngreso));
}
```

- `using var uow` arriba, apenas se necesita.
- Siempre `nameof(...)` para el método de origen — nunca strings mágicos.
- **Números y valores mágicos → constantes nombradas** (`Constants/` del módulo). Si ves un
  `== 1`, `is 4 or 10` o un `"confirmado"` suelto, dale nombre. El lector no debería adivinar
  qué significa un número.

Si un método pasa de ~40-50 líneas o mezcla dos flujos (p.ej. corporativo vs. normal),
partilo en privados con nombre. El nombre del privado es la documentación.

---

## 3. Validadores: un lugar, una regla por `if`

Toda la validación de negocio de un dominio va en **una** clase estática `*Validation`
(unificamos el sufijo; `*Validator` y `*Rules` quedan **deprecados** para código nuevo). Cada
regla es un `if` con guard clause y **un código de error único**, de modo que la regla de
negocio empata 1:1 con la línea de código.

```csharp
// ✅ Legible: se lee como una lista de reglas
public static OperationResult<bool> ValidarDireccion(Persona persona, string metodo)
{
    if (persona.CodigoPais is null or <= 0)
        return OperationResult<bool>.IsFailed("PER_DIR_01", metodo, "Falta país de residencia.", 400);

    if (persona.CodigoEstado is null or <= 0)
        return OperationResult<bool>.IsFailed("PER_DIR_02", metodo, "Falta estado/provincia.", 400);

    return OperationResult<bool>.Ok(true, metodo);
}
```

Un orquestador encadena sub-validaciones con el mismo patrón repetido (`if (!v.Success) return ...`),
lo que hace el flujo trivial de seguir. Modelo de referencia en la API hermana FDP:
`FichaDePersona/AppLogic/Utilities/PersonaValidation.cs`.

**Códigos de error:** prefijo de área + acción + número, único y grepable
(`FDB_SAI_01`, `PER_DIR_02`). No reutilices un código para dos reglas distintas.

---

## 4. Dónde va cada validación (un solo criterio)

| Dónde | Qué valida |
|-------|-----------|
| Controller | El **borde**: `request == null`, formato, tipos. Nada de reglas de negocio. |
| Validador / Service | Las **reglas de negocio** (existe, pertenece a la persona, estados válidos). |

No dupliques: el `request == null` va en el controller **o** en el servicio, no "a veces en
cada uno". La identidad del usuario **siempre** sale del token (`_currentUser`), nunca del body.

---

## 5. Manejo de excepciones: dejalas propagar

El `ExceptionHandlingMiddleware` centraliza el formateo de excepciones no controladas a
`OperationResult`. En servicios y controladores **no** pongas `try/catch` genérico.

- Sólo usá `try/catch` cuando necesites **transformar** el resultado, y documentá por qué
  (ej.: `AuthService.RecuperarPassword` responde 200 aunque falle, por seguridad — no filtrar
  si un mail existe).
- Si hay transacción DB: `catch { Rollback(); throw; }` — se relanza para que el middleware
  la formatee (ver DAT-01 en `AGENTS.md`).
- `catch (Exception)` (el genérico, último recurso) usa sufijo de código `_99`.

---

## 6. Convenciones mecánicas (las aplica el `.editorconfig`)

No hace falta memorizarlas: el IDE las sugiere. Para código nuevo:

- **Namespaces file-scoped** (`namespace AppLogic.Becas.Services;`). Los block-scoped
  existentes se migran al tocar el archivo, sin barrido masivo.
- **Primary constructors** en servicios y controladores nuevos
  (`public class FondoDeBecaService(IUnitOfWorkFactory uowFactory) : IFondoDeBecaServices`).
- Campos privados con prefijo `_camelCase`.
- **Idioma:** dominio en español (`ObtenerDetalleInscripcion`, `ValidarOfertasCompatibles`),
  infraestructura en inglés (`Success`, `HttpCode`, `GetByKey`). No mezclar dentro de un mismo
  identificador.
- **Interfaces en singular:** `IPersonaService`, `IInscripcionesService` (no `IXServices`).

---

## Checklist rápido (antes de un PR)

- [ ] ¿Alguna re-envoltura manual de `OperationResult`? → `Failure().As<T>()` o `return resultado;`
- [ ] ¿Números/strings mágicos? → constante nombrada.
- [ ] ¿El método se lee de corrido, sin anidamiento profundo? Si no, partilo.
- [ ] ¿La validación está en un solo lugar y con código de error único por regla?
- [ ] ¿`try/catch` genérico innecesario? → borralo, dejá propagar.
- [ ] ¿El IDE no marca warnings de estilo del `.editorconfig`?
