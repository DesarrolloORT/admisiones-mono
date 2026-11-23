---
status: accepted
owner: gabriele
updated: 2026-11-11
---

# ADR-046 — Becas: reconsideración por consulta y entrevista con la agenda del backoffice

> Fuente: [`api-admisiones/docs/DECISIONES-IMPLEMENTACION.md`](../../api-admisiones/docs/DECISIONES-IMPLEMENTACION.md), entradas del 2026-10-27 al 2026-10-30 (consulta, `canScheduleInterview`, agenda, entrevista, FBX, `ANULADA`, consultas ilimitadas, resultado de reconsideración) y 2026-11-11 10:19 (controles del alta FBX).

## Estado

`accepted` (2026-10-30; controles del alta FBX el 2026-11-11).

## Contexto

Con una beca Otorgada o No otorgada, la persona puede consultar la resolución. Si el backoffice la habilita, agenda una entrevista para que se reconsidere el porcentaje. Las tablas las crean el usuario y el DBA: `T_CONSULTA_BECA`, `T_RECONSIDERACION_BECA` y `T_AGENDA_BACKOFFICE*`. La reconsideración se registra como una inscripción a una prueba FBX (tipo 37), igual que el alta FBX del legacy.

## Decisión

- **Consulta (`POST scholarships/applications/inquiry`):** solo con `ESTADO_PARA_ADMISIONES` `OTORGADA` o `NO OTORGADA`. No hay límite de consultas, pero no puede haber dos **pendientes** para la misma `ID_PRUEBA` (`ESTADO_CONSULTA_BECA` 1 = pendiente, 2 = completado, lo cambia el backoffice). `ES_SEGUNDA_RECONSIDERACION` vale 1 si `ESTADO_EXTENSION` ya está resuelto.
- **Habilitación:** basta la fila en `T_RECONSIDERACION_BECA`. "Mis becas" agrega `inquirySent`, `canScheduleInterview`, `interviewScheduled` y `reconsideration { granted, newPercent? }`, solo en Granted/NotGranted.
- **Agenda = `T_AGENDA_BACKOFFICE`** (columna renombrada a `ID_AGENDA_BACKOFFICE`). La disponibilidad devuelve todos los horarios futuros en una sola llamada. Un horario libre es una agenda activa, una disponibilidad activa, un horario habilitado, futuro y sin reserva activa.
- **Reservar o reagendar (`POST`/`PUT .../interview`):**
  - inserta la FBX del mismo comienzo, buscada por `FECHA_ENTREGA_DJ_PRUEBA` = día del horario;
  - inserta la reserva y el participante `ALUMNO`;
  - al reagendar, la reserva anterior pasa a `ANULADA`, que es lo que acepta el CHECK.

  Solo se reagenda si nadie procesó la FBX; si no, 409 `InterviewAlreadyProcessed`.
- **Controles del alta FBX del legacy** (`Rules/FbxEnrollmentRules`), antes de insertar. Para el front hay un solo 409, `Scholarship.InterviewCannotSchedule`, y la regla puntual va a un warning en el log: son problemas de datos que el postulante no puede arreglar. La regla 11 se aplica **tal cual el documento**, aunque parezca invertida.
- La concurrencia sobre el horario se resuelve con [ADR-036](ADR-036-candado-distribuido.md).

## Consecuencias

- El lugar de la entrevista sale de `T_PRUEBA.ID_LOCALIDAD` → `T_LOCALIDAD`. El entrevistador sale de `T_AGENDA_BACKOFFICE_PERSONA`.
- `SEGUNDA_EXTENSION_OTORGADA = 'FALSE'` no distingue "no otorgada" de "no hubo segunda". Hasta que la base marque esa diferencia, la card "No es posible ampliar tu beca" sigue mostrando "Consultar".
- **Pendientes:**
  - filtrar al entrevistador por `TIPO_RELACION`;
  - confirmar con negocio la columna de fecha de la FBX;
  - poner un UNIQUE en Oracle sobre la reserva activa;
  - confirmar que `ID_CONSULTA_BECA` es identity en todos los ambientes.
- Contrato: `becas.contract.json`.

## Referencias

- [ADR-024](ADR-024-mis-becas-estado-desde-vista.md): "Mis becas".
- [ADR-021](ADR-021-becas-paridad-escrituras-legacy.md): paridad con el legacy.
- `api-admisiones/WebApiAdmisiones/AppLogic/AppLogic.Scholarships/README.md`
