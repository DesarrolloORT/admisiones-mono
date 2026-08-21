# AppLogic.Integrations.EnrollmentsAndPayments

**Nivel 2 — depende de `AppLogic.Contracts` y `Core/Utilities`.**

## Qué resuelve

Cliente HTTP de la **API interna de Inscripciones y Pagos** de ORT (`ORTSecure/...`). Es el único
lugar del sistema que le habla a ese servicio.

## Qué expone

`IEnrollmentsAndPaymentsApiClient`, con 7 operaciones:

| Método | Endpoint remoto |
|---|---|
| `ConfirmMultiplePreEnrollmentAsync` | confirma la preinscripción de varias ofertas de una |
| `GetMinimumDepositAsync` | `Inscripciones/SeniaMinima` — seña mínima de una inscripción |
| `GetCurrentAccountAsync` | `Pagos/CtaCte` — estado de cuenta |
| `GetCoursePaymentsAsync` | pagos por curso |
| `GetCartsByEnrollmentAsync` | `Pagos/Carritos` |
| `PayCartsByEnrollmentAsync` | `Pagos/Carritos/Pagar` |
| `GetCreateInvoiceUrlByEnrollmentAsync` | `Pagos/Carritos/UrlCrearFactura` |

El `HttpClient` se registra en el host (`AddEnrollmentsAndPaymentsApiClient`), no acá, porque la
configuración de la URL base y el handler son decisión del composition root.

## ⚠️ Los DTOs de este proyecto están en español A PROPÓSITO

`ResumenInscripcionApiDto`, `CursosPagosResponse`, `CtaCteResponse`,
`SeniaMinimaApiResponse`, `CarritoPagoReservaApiDto`, `DtoTurno`, `ConfirmarPreInscripcionMultipleApi*`…
todos tienen propiedades como `IdOferta`, `Comienzo`, `SaldoActual`, `ValorSeniaMinima`.

**No los traduzcas.** Modelan el formato exacto que emite un sistema ajeno. Si les cambiás el nombre,
la deserialización deja de mapear y los campos quedan en null — sin error de compilación y sin
excepción en runtime.

`CartPaymentMessage` conserva además `[JsonPropertyName("clave")]` / `("valor")` por la misma razón,
con el comentario explicándolo en el propio archivo.

## ⚠️ Las claves de query string también son contrato

Las URLs llevan `?idProducto=`, `&idProceso=`, `?tipoPago=`, `&banco=`, `&idInscripto=`. **Están en
español y así tienen que quedar**: las define el servicio remoto.

Ya pasó una vez que un rename masivo las cambió a inglés. Compilaba, los tests pasaban (porque los
asserts se "corrigieron" solos) y habría roto todos los pagos en producción. Si tocás este archivo
con un `sed`, revisá después:

```bash
grep -n 'idProducto\|idProceso\|tipoPago\|banco' Services/EnrollmentsAndPaymentsApiClient.cs
```

## Ninguno de estos DTOs sale al front

Ese es el límite anticorrupción. Cada módulo consumidor mapea a su propio DTO en inglés:

- `CartPaymentMessage` → `Enrollments.PaymentMessage` (vía `EnrollmentMapper.ToPaymentMessages`)

Está cubierto por `RenamedDtoJsonContractTests`, que verifica las dos direcciones: que los DTOs
propios serializan en inglés y que `CartPaymentMessage` sigue deserializando el formato español.

## Estructura

- `Dtos/` — el formato del sistema remoto (español).
- `Services/EnrollmentsAndPaymentsApiClient` — el cliente. Toda llamada pasa por `SendAsync`, que
  centraliza el manejo de errores y la traducción a `OperationResult`.
