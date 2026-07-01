using AppLogic.Constants;
using AppLogic.Dtos.EncuestaInicial;
using AppLogic.Dtos.Tivenos;
using AppLogic.Helpers;
using AppLogic.Helpers.ValidationHelpers;
using AppLogic.IServices.Catalogos;
using AppLogic.IServices.Tivenos;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using ConnectionContext;
using Utilities;

namespace AppLogic.Services.Inscripciones.Encuesta
{
    internal sealed class EncuestaInicialService(
        IUnitOfWorkFactory uowFactory,
        IDbConnectionContext dbConnectionContext,
        IGeneralService generalService,
        ITivenosEnvioService tivenosEnvioService)
    {
        private const long CodigoOrientacionQuintoLegacy = 1304;

        public OperationResult<DtoGuardarEncuestaInicialResponse> GuardarEncuestaInicial(
            long codigoPersona,
            DtoGuardarEncuestaInicialRequest request)
        {
            using var uow = uowFactory.Create();

            var persona = uow.Personas.GetByKey(codigoPersona);
            if (persona == null)
            {
                return OperationResult<DtoGuardarEncuestaInicialResponse>.IsFailed(
                    "INS_EI_01",
                    nameof(GuardarEncuestaInicial),
                    PersonaConstants.PersonaNoEncontradaMessage,
                    404);
            }

            var validacion = EncuestaInicialCatalogValidator.ValidarRequestParcial(uow, request, nameof(GuardarEncuestaInicial));
            if (!validacion.Success)
            {
                return OperationResult<DtoGuardarEncuestaInicialResponse>.IsFailed(
                    validacion.ErrorCode,
                    nameof(GuardarEncuestaInicial),
                    validacion.Message,
                    validacion.HttpCode);
            }

            var contexto = ResolverContextoAdmision(uow, codigoPersona, request);
            if (!contexto.Success)
            {
                return OperationResult<DtoGuardarEncuestaInicialResponse>.IsFailed(
                    contexto.ErrorCode,
                    nameof(GuardarEncuestaInicial),
                    contexto.Message,
                    contexto.HttpCode);
            }

            var encuesta = ObtenerEncuestaParaGuardar(uow, codigoPersona, request, contexto.Data);
            var esNueva = encuesta == null;
            encuesta ??= CrearEncuestaInicial(persona, codigoPersona);

            uow.BeginTransaction();
            try
            {
                EncuestaInicialMapper.AplicarDatosTecnicos(
                    encuesta,
                    persona,
                    codigoPersona,
                    request.CarreraId ?? encuesta.IdProducto,
                    request.ProcesoId ?? encuesta.IdProceso,
                    contexto.Data?.IdComienzo ?? encuesta.IdComienzo,
                    contexto.Data?.IdTurno ?? encuesta.IdTurno);
                EncuestaInicialMapper.AplicarEducacion(encuesta, request);
                EncuestaInicialMapper.AplicarDecisionAcademica(encuesta, request);
                EncuestaInicialMapper.AplicarExperienciaOrt(encuesta, request);
                var actualizaPersona = EncuestaInicialMapper.AplicarSituacionLaboral(persona, request);

                if (esNueva)
                    uow.EncuestaIniAdmisions.Add(encuesta);
                else
                    uow.EncuestaIniAdmisions.Update(encuesta);

                EncuestaInicialChildTablesService.AplicarListasHijas(uow, dbConnectionContext, codigoPersona, request);
                if (actualizaPersona)
                    uow.Personas.Update(persona);

                uow.Save();

                var encuestaPersistida = uow.EncuestaIniAdmisions.GetByKey(encuesta.IdEncuestaIni) ?? encuesta;
                var personaPersistida = uow.Personas.GetByKey(codigoPersona) ?? persona;
                var response = EncuestaInicialValidator.ValidarCompletitudDesdeBase(uow, encuestaPersistida, personaPersistida, codigoPersona);

                encuesta.EstadoEncuestaIniAdmision = response.Estado;
                if (response.Estado == EncuestaInicialState.EstadoDefinitivo)
                {
                    var finalizacion = FinalizarEncuestaDefinitiva(uow, encuesta, codigoPersona);
                    if (!finalizacion.Success)
                    {
                        uow.Rollback();
                        return OperationResult<DtoGuardarEncuestaInicialResponse>.IsFailed(
                            finalizacion.ErrorCode,
                            nameof(GuardarEncuestaInicial),
                            finalizacion.Message,
                            finalizacion.HttpCode);
                    }
                }

                uow.EncuestaIniAdmisions.Update(encuesta);
                uow.Save();
                uow.Commit();

                return OperationResult<DtoGuardarEncuestaInicialResponse>.Ok(response, nameof(GuardarEncuestaInicial));
            }
            catch
            {
                uow.Rollback();
                throw;
            }
        }

        private static OperationResult<ContextoAdmision?> ResolverContextoAdmision(
            IUnitOfWork uow,
            long codigoPersona,
            DtoGuardarEncuestaInicialRequest request)
        {
            if (!request.CarreraId.HasValue && !request.ProcesoId.HasValue)
                return OperationResult<ContextoAdmision?>.Ok(null, nameof(GuardarEncuestaInicial));

            if (!request.CarreraId.HasValue || !request.ProcesoId.HasValue)
                return OperationResult<ContextoAdmision?>.Ok(null, nameof(GuardarEncuestaInicial));

            var ofertas = uow.InteresProductoOfertas.GetOfertasSeleccionadas(
                codigoPersona,
                request.CarreraId.Value,
                request.ProcesoId.Value);

            if (ofertas.Count == 0)
            {
                return OperationResult<ContextoAdmision?>.IsFailed(
                    "INS_EI_53",
                    nameof(GuardarEncuestaInicial),
                    "No existe oferta/interes activo para la persona, producto y proceso indicados.",
                    404);
            }

            if (ofertas.Count > 1)
            {
                return OperationResult<ContextoAdmision?>.IsFailed(
                    "INS_EI_54",
                    nameof(GuardarEncuestaInicial),
                    "Existe mas de una oferta/interes activo para la persona, producto y proceso indicados.",
                    409);
            }

            var oferta = ofertas.Single();
            return OperationResult<ContextoAdmision?>.Ok(
                new ContextoAdmision(
                    oferta.Supraoferta?.IdComienzo,
                    oferta.IdTurno),
                nameof(GuardarEncuestaInicial));
        }

        private static EncuestaIniAdmision? ObtenerEncuestaParaGuardar(
            IUnitOfWork uow,
            long codigoPersona,
            DtoGuardarEncuestaInicialRequest request,
            ContextoAdmision? contexto)
        {
            if (request.CarreraId.HasValue && contexto?.IdComienzo.HasValue == true)
            {
                var encuestaPorProductoComienzo = uow.EncuestaIniAdmisions.GetByPersonaProductoComienzo(
                    codigoPersona,
                    request.CarreraId.Value,
                    contexto.IdComienzo.Value);
                if (encuestaPorProductoComienzo != null)
                    return encuestaPorProductoComienzo;
            }

            return uow.EncuestaIniAdmisions.GetByPersona(codigoPersona);
        }

        private EncuestaIniAdmision CrearEncuestaInicial(Persona persona, long codigoPersona)
        {
            return new EncuestaIniAdmision
            {
                IdEncuestaIni = dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_ENCUESTA_INI_ADMISION),
                FechaEncuestaIni = dbConnectionContext.CurrentDateTime(),
                TipoDocumento = persona.TipoDocumento,
                Documento = persona.Documento,
                CodigoPersona = codigoPersona,
                TipoInscripcion = EncuestaInicialState.TipoInscripcionSoloEncuesta,
                NuevaversionEncuestaIni = CommonConstants.Booleanos.Si,
                EstadoEncuestaIniAdmision = EncuestaInicialState.EstadoTemporal
            };
        }

        private OperationResult<bool> FinalizarEncuestaDefinitiva(
            IUnitOfWork uow,
            EncuestaIniAdmision encuesta,
            long codigoPersona)
        {
            if (encuesta.IdProceso.HasValue && encuesta.IdProducto.HasValue)
            {
                var fechaVencimientoResult = generalService.CalcularFechaVencimientoAdmisiones(uow, codigoPersona, encuesta.IdProceso.Value);
                if (!fechaVencimientoResult.Success)
                {
                    return OperationResult<bool>.IsFailed(
                        fechaVencimientoResult.ErrorCode,
                        nameof(GuardarEncuestaInicial),
                        fechaVencimientoResult.Message,
                        fechaVencimientoResult.HttpCode);
                }

                encuesta.FechaVtoAdmision = fechaVencimientoResult.Data;
            }

            return SincronizarBachilleratoPersona(uow, encuesta, codigoPersona, nameof(GuardarEncuestaInicial));
        }

        private OperationResult<bool> SincronizarBachilleratoPersona(
            IUnitOfWork uow,
            EncuestaIniAdmision encuesta,
            long codigoPersona,
            string methodName)
        {
            var datos = ObtenerDatosBachilleratoDefinitivo(encuesta, methodName);
            if (!datos.Success)
            {
                return OperationResult<bool>.IsFailed(datos.ErrorCode, methodName, datos.Message, datos.HttpCode);
            }

            var datosBachillerato = datos.Data!;
            var fechaActual = dbConnectionContext.CurrentDateTime();
            var existente = uow.BachilleratoPersonas.GetByKey(codigoPersona);
            if (existente == null)
            {
                uow.BachilleratoPersonas.Add(new BachilleratoPersona
                {
                    CodigoPersona = codigoPersona,
                    CodigoInstitucion = datosBachillerato.CodigoInstitucion,
                    AnioBachillerPer = datosBachillerato.AnioBachiller,
                    CodigoOrientacion = datosBachillerato.CodigoOrientacion,
                    ActualizacionBachillerPer = fechaActual
                });

                return tivenosEnvioService.EncolarAltaDatosBachillerato(
                    uow,
                    new DtoTivenosBachilleratoRequest
                    {
                        CodigoPersona = codigoPersona,
                        CodigoOrientacion = datosBachillerato.CodigoOrientacion
                    },
                    dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_TIVENOS),
                    methodName);
            }

            if (!CambioBachillerato(existente, datosBachillerato))
                return OperationResult<bool>.Ok(false, methodName);

            existente.CodigoInstitucion = datosBachillerato.CodigoInstitucion;
            existente.AnioBachillerPer = datosBachillerato.AnioBachiller;
            existente.CodigoOrientacion = datosBachillerato.CodigoOrientacion;
            existente.ActualizacionBachillerPer = fechaActual;
            uow.BachilleratoPersonas.Update(existente);

            return tivenosEnvioService.EncolarModificacionDatosBachillerato(
                uow,
                new DtoTivenosBachilleratoRequest
                {
                    CodigoPersona = codigoPersona,
                    CodigoOrientacion = datosBachillerato.CodigoOrientacion
                },
                dbConnectionContext.NextId(DbConnectionContext.DbConnectionContextType.TO_TIVENOS),
                methodName);
        }

        private static OperationResult<DatosBachilleratoPersona> ObtenerDatosBachilleratoDefinitivo(
            EncuestaIniAdmision encuesta,
            string methodName)
        {
            if (!long.TryParse(encuesta.UltimoAnioSextoEncuestaIni, out var ultimoAnio))
            {
                return OperationResult<DatosBachilleratoPersona>.IsFailed(
                    "INS_EI_36",
                    methodName,
                    "No se pudo resolver el anio de bachillerato para la persona.",
                    400);
            }

            ultimoAnio = ResolverUltimoAnioBachilleratoLegacy(ultimoAnio);
            if (ultimoAnio is < 4 or > 6)
            {
                return OperationResult<DatosBachilleratoPersona>.IsFailed(
                    "INS_EI_37",
                    methodName,
                    "Anio de bachillerato invalido para la persona.",
                    400);
            }

            var codigoOrientacion = ultimoAnio switch
            {
                4 => null,
                5 => CodigoOrientacionQuintoLegacy,
                6 => encuesta.CodigoTitulo,
                _ => null
            };

            if (ultimoAnio == 6 && (!codigoOrientacion.HasValue || codigoOrientacion.Value <= 0))
            {
                return OperationResult<DatosBachilleratoPersona>.IsFailed(
                    "INS_EI_38",
                    methodName,
                    "No se pudo resolver la orientacion de bachillerato para la persona.",
                    400);
            }

            return OperationResult<DatosBachilleratoPersona>.Ok(
                new DatosBachilleratoPersona(encuesta.CodigoInstitucionBac, ultimoAnio.ToString(), codigoOrientacion),
                methodName);
        }

        private static long ResolverUltimoAnioBachilleratoLegacy(long value)
            => value is >= 10 and <= 12 ? value - 6 : value;

        private static bool CambioBachillerato(BachilleratoPersona existente, DatosBachilleratoPersona datos)
        {
            return existente.CodigoInstitucion != datos.CodigoInstitucion
                || !string.Equals(existente.AnioBachillerPer, datos.AnioBachiller, StringComparison.Ordinal)
                || existente.CodigoOrientacion != datos.CodigoOrientacion;
        }

        private sealed record ContextoAdmision(long? IdComienzo, long? IdTurno);

        private sealed record DatosBachilleratoPersona(
            long? CodigoInstitucion,
            string AnioBachiller,
            long? CodigoOrientacion);
    }
}
