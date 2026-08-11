# Admisiones y Tivenos

## Resumen

`WebApiAdmisiones` no llama directo a Tivenos. Igual que la API legacy, solo genera registros en `T_ENVIO_PARA_TIVENOS`.

El envio real lo hace `ServicioInterno`, que levanta esa cola, completa datos desde ORT y pega a la API de Tivenos.

Tambien existe el flujo inverso: Tivenos pega a `WebApiTivenos`, que guarda lo recibido en ORT y actualiza personas/intereses segun el metodo.

## Flujo Admisiones -> Tivenos

1. Un endpoint de `WebApiAdmisiones` ejecuta una accion del sitio.
2. `BusinessAdmisiones.AdmAdmisiones` arma un `EnvioParaTivenos`.
3. Se inserta en `T_ENVIO_PARA_TIVENOS` con `Status = "Nuevo"`.
4. Algun proceso invoca `ServicioInterno`.
5. `ServicioInterno` toma pendientes, los marca `PROCESANDO`, completa datos y llama a Tivenos.
6. Se actualiza el estado del envio y se registra auditoria en `T_INVOCACION_WS_TIVENOS`.

Ubicaciones:

- Encolado desde Admisiones: `C:\GIT\LogicaORT\BusinessAdmisiones\AdmAdmisiones.cs:1142`
- Entidad/insert de cola: `C:\GIT\LogicaORT\Core\Business\EnvioParaTivenos.cs:2975`
- Endpoint de Servicio Interno: `C:\GIT\LogicaORT\ServicioInterno\Controllers\TivenosController.cs:92`
- Procesamiento de cola: `C:\GIT\LogicaORT\BusinessServicioInterno\AdmServicioInterno.cs:2166`
- Llamada real a Tivenos: `C:\GIT\LogicaORT\BusinessServicioInterno\AdmServicioInterno.cs:1656`

## Metodos que encola Admisiones

| Metodo enviado a Tivenos | Endpoint legacy | Donde se genera |
|---|---|---|
| `DesinteresProductoxAlta` | `ORT/Persona/Paso3` | `AdmAdmisiones.cs:460`, encola en `:476` |
| `RegistroDesdeSitioAdmisiones` | `ORT/Persona/Paso5` | `AdmAdmisiones.cs:836`, encola en `:850` |
| `AltaInteresXSeleccionEnSitio` | `ORTSecure/General/InteresProducto` | `AdmAdmisiones.cs:1300`, `:1386`, `:1440`; encola en `:1316`, `:1402`, `:1456` |
| `AltaDatosBachillerato` | `ORTSecure/General/ConfirmarPreInscripcion` | `AdmAdmisiones.cs:2380`, encola en `:2392` |
| `ModificacionDatosBachillerato` | `ORTSecure/General/ConfirmarPreInscripcion` | `AdmAdmisiones.cs:2442`, encola en `:2454` |
| `CI_Enviada` | `ORTSecure/General/ConfirmarPreInscripcion` | `AdmAdmisiones.cs:2887`, encola en `:2901` |

Notas:

- La legacy `WebApiAdmisiones` referencia `BusinessAdmisiones`, no `BusinessApiTivenos`.
- `Beca_Seleccion` y `Beca_CompletaDJ` estan comentados en `AdmAdmisiones`, pero `Beca_CompletaDJ`
  igual se encola desde `Inscripto.ConfirmacionDeclaracionJuradaWeb:5176` (via
  `ConfirmarPreInscripcionMultiple`), y `AltaInscripcion` desde
  `ModInterBecasSGI.DarAltaEnvioParaTivenosDesdeInscripcion:140` por la misma cadena.
- Antes de insertar en `T_ENVIO_PARA_TIVENOS`, se valida el parametro `SE_LIBERO_TIVENOS`
  (`Core/Business/EnvioParaTivenos.cs:2980`). Aplica a todas las ramas, incluida la API nueva.

## Metodos que encola la API nueva (`api-admisiones`)

| Metodo enviado a Tivenos | Endpoint | Donde se encola |
|---|---|---|
| `RegistroDesdeSitioAdmisiones` | `POST enrollments/product-interest` | `RegisterProductInterest.RegisterAndEnqueue` |
| `AltaInteresXSeleccionEnSitio` | `POST enrollments/product-interest` | `RegisterProductInterest.RegisterAndEnqueue` |
| `AltaDatosBachillerato` | `POST enrollments/initial-survey` | `InitialSurveyService` |
| `ModificacionDatosBachillerato` | `POST enrollments/initial-survey` | `InitialSurveyService` |

`RegistroDesdeSitioAdmisiones` cambio de lugar respecto al legacy: salia del Paso5
(`AdmAdmisiones.AgregarPersonaInteres:836`), al insertar la fila de `T_PERSONA`. En el sitio nuevo
`POST registration/confirm-new-person` no crea la persona -- los datos van a Redis y la persona se
inserta al establecer la contraseña, momento en el que todavia no hay `ProcesoId` ni `ProductoId`.
Por eso se encola en el paso 1 de inscripcion, junto con `AltaInteresXSeleccionEnSitio` y en la
misma transaccion, solo cuando es el primer ingreso de la persona a admisiones
(`T_PERSONA_ADMITE.FECHA_FRESCO_PERSONA_ADMITE` sin cargar).

`CI_Enviada`, `AltaInscripcion` y `Beca_CompletaDJ` siguen saliendo de la legacy
`WebApiInscripcionesPagos` (`ConfirmarPreInscripcion[Multiple]`); `POST enrollments/confirm-pre-enrollment`
todavia no los encola.

## Quien dispara Servicio Interno

El endpoint interno es:

```text
POST ORT/Tivenos/EnvioDeDatosDeOrtParaTivenos
```

En el codigo revisado hay helpers que saben invocarlo:

- `C:\GIT\LogicaORT\BusinessGeneral\AdmGeneral.cs:30`
- `C:\GIT\LogicaORT\BusinessGeneral\AdmAutenticacionAutorizacion.cs:36`
- `C:\GIT\LogicaORT\BusinessApiTivenos\AdmApiTivenos.cs:3218`

En `BusinessAdmisiones\AdmAdmisiones.cs` las llamadas a `InvocaServiciosAltaEnvioParaTivenos()` estan comentadas. No se encontro un scheduler/job activo en el repo; probablemente lo dispara una tarea externa, proceso operativo o llamada manual.

## Flujo Tivenos -> ORT

Tivenos entra por:

```text
POST ORTSecure/Tivenos/AltaTivenos
```

Ubicaciones:

- Controller: `C:\GIT\LogicaORT\WebApiTivenos\Controllers\TivenosController.cs:39`
- Logica: `C:\GIT\LogicaORT\BusinessApiTivenos\AdmApiTivenos.cs:385`

`AgregarTivenos` primero inserta el payload en `T_TIVENOS` y despues procesa segun `dtoTivenos.Metodo`.

| Metodo recibido desde Tivenos | Que hace |
|---|---|
| `AsociarDocumentoTivenosCodigoSape` | Asocia documento de Tivenos con codigo SAPE/ORT. |
| `ModificacionDatosPersonaConCodigoSAPE` | Actualiza datos de persona y bachillerato. |
| `NuevoInteres` | Inserta/actualiza interes con grado `4`. |
| `BajaInteres` | Actualiza interes producto con grado `0`. |
| `InteresadoTuvoRAS` | Registra que el interesado tuvo RAS. |

Tablas principales que puede modificar este flujo:

- `T_TIVENOS`
- `T_PERSONA`
- `T_BACHILLERATO_PERSONA`
- `T_INTERES`
- `T_INTERES_PRODUCTO`
- `T_TUVO_RAS_TIVENOS`
- `T_PERSONA_CONOCE_TIVENOS`
- `T_DOCUMENTO_UNICO_TIVENOS`
