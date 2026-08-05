# AppLogic.Integrations.Tivenos

**Nivel 2 — depende de `AppLogic.Contracts` y `BusinessLogic`.**

## Qué resuelve

Tivenos es el **CRM** de ORT. Este módulo encola los mensajes que le avisan de un interés por
producto o de un cambio en los datos de bachillerato de una persona.

Es una integración **asincrónica por tabla**: no hace HTTP. Escribe filas en `T_ENVIO_PARA_TIVENOS`
y otro proceso las levanta. Por eso "encolar" (`Enqueue*`) y no "enviar".

## Qué expone

`ITivenosQueueService`, con tres operaciones:

| Método | Cuándo se llama |
|---|---|
| `EnqueueProductInterestFromSiteSelection` | La persona marca interés por un producto desde el sitio. |
| `EnqueueHighSchoolDataCreation` | Se cargan por primera vez los datos de bachillerato. |
| `EnqueueHighSchoolDataUpdate` | Se modifican datos de bachillerato ya cargados. |

Se registra con `services.AddTivenosIntegration()`.

## Estructura

- `Dtos/` — el formato que espera Tivenos: `DtoTivenosAltaInteresRequest`,
  `DtoTivenosBachilleratoRequest`, `TivenosAltaInteresOperacion`.
- `Mapping/TivenosMessageMapper` — traduce del dominio de admisiones al formato de Tivenos.
- `Services/TivenosQueueService` — arma la fila y la inserta.

## Trampas

- **Los nombres de los DTOs están en español a propósito** (`CodigoPersona`, `Disparador`,
  `OrigenLlamador`, `TipoProcesoLlamador`, `CodigoOrientacion`). Describen el formato que consume un
  sistema ajeno. Traducirlos rompe la integración. Es la excepción documentada en el glosario.
- **Encolar no es enviar.** Si el mensaje no llega a Tivenos, el problema puede estar en el proceso
  consumidor, no acá. Este módulo solo garantiza que la fila quedó escrita.
- `TivenosQueueService` devuelve `bool`, no `OperationResult`: para el llamador solo importa si se
  encoló o no, y un fallo acá **no debe abortar** la operación de negocio que lo disparó (registrar
  el interés en admisiones es lo importante; avisarle al CRM es secundario).

## Quién lo usa

Solo `AppLogic.Enrollments`, desde el registro de interés por producto y desde la encuesta inicial.
