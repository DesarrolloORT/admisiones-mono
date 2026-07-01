using AppLogic.Dtos.EncuestaInicial;
using AppLogic.Helpers;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Helpers.ValidationHelpers
{
    public static class EncuestaInicialCatalogValidator
    {
        public static OperationResult<bool> ValidarRequestParcial(
            IUnitOfWork uow,
            DtoGuardarEncuestaInicialRequest request,
            string methodName)
        {
            if (request == null)
                return OperationResult<bool>.IsFailed("INS_EI_02", methodName, "Request invalido.", 400);

            var fixedValidation = ValidarOpcionesFijas(request, methodName);
            if (!fixedValidation.Success)
                return fixedValidation;

            var dynamicValidation = ValidarCatalogosDinamicos(uow, request, methodName);
            if (!dynamicValidation.Success)
                return dynamicValidation;

            return ValidarConsistenciaInterna(request, methodName);
        }

        private static OperationResult<bool> ValidarOpcionesFijas(
            DtoGuardarEncuestaInicialRequest request,
            string methodName)
        {
            if (!EncuestaInicialOpciones.Contiene(EncuestaInicialOpciones.UbicacionesUltimoAnioSecundaria, request.UbicacionUltimoAnioSecundariaId))
                return OperationResult<bool>.IsFailed("INS_EI_14", methodName, "Ultimo anio de secundaria invalido.", 400);
            if (!EncuestaInicialOpciones.Contiene(EncuestaInicialOpciones.EstadosEducacionSuperiorPrevia, request.EstadoEducacionSuperiorPreviaId))
                return OperationResult<bool>.IsFailed("INS_EI_50", methodName, "Educacion superior previa invalida.", 400);
            if (!EncuestaInicialOpciones.Contiene(EncuestaInicialOpciones.NivelesFormacionTutores, request.NivelFormacionPadreTutorId))
                return OperationResult<bool>.IsFailed("INS_EI_08", methodName, "Instruccion padre invalida.", 400);
            if (!EncuestaInicialOpciones.Contiene(EncuestaInicialOpciones.NivelesFormacionTutores, request.NivelFormacionMadreTutorId))
                return OperationResult<bool>.IsFailed("INS_EI_07", methodName, "Instruccion madre invalida.", 400);
            if (!EncuestaInicialOpciones.Contiene(EncuestaInicialOpciones.AniosEducacionMediaSuperior, request.AnioDecisionCarreraId))
                return OperationResult<bool>.IsFailed("INS_EI_09", methodName, "Decision de carrera invalida.", 400);
            if (!EncuestaInicialOpciones.Contiene(EncuestaInicialOpciones.AniosEducacionMediaSuperior, request.AnioDecisionOrtId))
                return OperationResult<bool>.IsFailed("INS_EI_10", methodName, "Decision de universidad invalida.", 400);
            if (!EncuestaInicialOpciones.Contiene(EncuestaInicialOpciones.ApoyosDecision, request.ApoyoDecisionId))
                return OperationResult<bool>.IsFailed("INS_EI_11", methodName, "Con quien compartio la decision invalido.", 400);
            if (!EncuestaInicialOpciones.Contiene(EncuestaInicialOpciones.NivelesDecision, request.NivelDecisionId))
                return OperationResult<bool>.IsFailed("INS_EI_15", methodName, "Nivel de decision invalido.", 400);
            if (!EncuestaInicialOpciones.Contiene(EncuestaInicialOpciones.TiposJornada, request.TipoJornadaId))
                return OperationResult<bool>.IsFailed("INS_EI_48", methodName, "Tipo jornada invalido.", 400);
            if (!EncuestaInicialOpciones.Contiene(EncuestaInicialOpciones.Valoraciones, request.ValoracionAsesoramientoOrtId))
                return OperationResult<bool>.IsFailed("INS_EI_16", methodName, "Valoracion de asesoramiento invalida.", 400);
            if (!EncuestaInicialOpciones.Contiene(EncuestaInicialOpciones.Valoraciones, request.ValoracionSitioWebOrtId))
                return OperationResult<bool>.IsFailed("INS_EI_17", methodName, "Valoracion del sitio web invalida.", 400);
            if (!EncuestaInicialOpciones.Contiene(EncuestaInicialOpciones.Valoraciones, request.ValoracionInstalacionesOrtId))
                return OperationResult<bool>.IsFailed("INS_EI_18", methodName, "Valoracion de instalaciones invalida.", 400);

            if (request.ProcesoId is <= 0)
                return OperationResult<bool>.IsFailed("INS_EI_05", methodName, "Proceso invalido.", 400);
            if (request.OrientacionBachilleratoId is <= 0)
                return OperationResult<bool>.IsFailed("INS_EI_21", methodName, "Titulo invalido.", 400);

            return OperationResult<bool>.Ok(true, methodName);
        }

        private static OperationResult<bool> ValidarCatalogosDinamicos(
            IUnitOfWork uow,
            DtoGuardarEncuestaInicialRequest request,
            string methodName)
        {
            if (request.CarreraId.HasValue && !uow.Productos.EsProductoValidoParaInteres(request.CarreraId.Value))
                return OperationResult<bool>.IsFailed("INS_EI_03", methodName, "El producto indicado es invalido.", 400);

            var procesosDisponibles = uow.VdProcesosDisponibles1y2s;
            if (request.CarreraId.HasValue
                && request.ProcesoId.HasValue
                && procesosDisponibles != null
                && !procesosDisponibles.GetProcesosDisponibles(request.CarreraId.Value).Any(p => p.IdProceso == request.ProcesoId.Value))
            {
                return OperationResult<bool>.IsFailed("INS_EI_34", methodName, "No existe un proceso habilitado para el producto indicado.", 404);
            }

            if (request.AnioBachillerato.HasValue && ResolverAnioBachiller(uow, request.AnioBachillerato.Value) == null)
                return OperationResult<bool>.IsFailed("INS_EI_06", methodName, "Ultimo anio de bachillerato invalido.", 400);

            if (request.OrientacionBachilleratoId.HasValue && !TituloCatalogado(uow, request.OrientacionBachilleratoId.Value))
                return OperationResult<bool>.IsFailed("INS_EI_22", methodName, "El titulo indicado es invalido.", 400);

            if (request.InstitucionSecundariaId is <= 0)
                return OperationResult<bool>.IsFailed("INS_EI_19", methodName, "Institucion invalida.", 400);
            if (request.InstitucionSecundariaId.HasValue && uow.Empresas.GetByKey(request.InstitucionSecundariaId.Value) == null)
                return OperationResult<bool>.IsFailed("INS_EI_20", methodName, "La institucion indicada es invalida.", 400);

            var empresas = ValidarEmpresas(uow, request.UniversidadConsideradaIds, methodName);
            if (!empresas.Success)
                return empresas;

            empresas = ValidarEmpresas(uow, request.UniversidadEducacionSuperiorIds, methodName);
            if (!empresas.Success)
                return empresas;

            if (request.MotivoEleccionOrtIds is { Count: > 0 })
            {
                var motivos = uow.MotivoOpcionesAdmisions.GetAll();
                if (request.MotivoEleccionOrtIds.Any(id => id <= 0 || !motivos.Any(m => m.IdMotivo == id)))
                    return OperationResult<bool>.IsFailed("INS_EI_23", methodName, "Motivo de eleccion invalido.", 400);
            }

            if (request.PublicidadOrtIds is { Count: > 0 })
            {
                var publicidades = uow.PublicidadOpcionesAdmisions.GetAll();
                if (request.PublicidadOrtIds.Any(id => id <= 0 || !publicidades.Any(p => p.IdPublicidad == id)))
                    return OperationResult<bool>.IsFailed("INS_EI_24", methodName, "Publicidad seleccionada invalida.", 400);
            }

            return OperationResult<bool>.Ok(true, methodName);
        }

        private static OperationResult<bool> ValidarConsistenciaInterna(
            DtoGuardarEncuestaInicialRequest request,
            string methodName)
        {
            if (request.RecursaAnioBachillerato == true && !request.VecesRecursaAnioBachillerato.HasValue)
                return OperationResult<bool>.IsFailed("INS_EI_52", methodName, "Debe indicar veces que recursa el anio de bachillerato.", 400);

            return OperationResult<bool>.Ok(true, methodName);
        }

        private static OperationResult<bool> ValidarEmpresas(
            IUnitOfWork uow,
            List<long>? empresas,
            string methodName)
        {
            if (empresas == null)
                return OperationResult<bool>.Ok(true, methodName);

            var universidades = uow.Empresas.GetUniversidades();
            if (empresas.Any(id => id <= 0))
                return OperationResult<bool>.IsFailed("INS_EI_25", methodName, "Universidad seleccionada invalida.", 400);
            if (empresas.Any(id => !universidades.Any(u => u.CodigoEmpresa == id)))
                return OperationResult<bool>.IsFailed("INS_EI_27", methodName, "Universidad seleccionada invalida.", 400);

            return OperationResult<bool>.Ok(true, methodName);
        }

        internal static AnioBachiller? ResolverAnioBachiller(IUnitOfWork uow, long value)
        {
            return uow.AnioBachillers.GetAllWithRelated()
                .FirstOrDefault(a => EsAnioBachiller(a, value));
        }

        internal static bool AnioBachillerTieneOrientaciones(IUnitOfWork uow, long value)
            => ResolverAnioBachiller(uow, value)?.Titulos.Count > 0;

        private static bool TituloCatalogado(IUnitOfWork uow, long codigoTitulo)
        {
            return uow.AnioBachillers.GetAllWithRelated()
                .SelectMany(a => a.Titulos)
                .Any(t => t.CodigoTitulo == codigoTitulo);
        }

        private static bool EsAnioBachiller(AnioBachiller anio, long value)
        {
            return anio.IdAnioBachiller == value
                || anio.CantAniosAnioBachiller == value;
        }
    }
}
