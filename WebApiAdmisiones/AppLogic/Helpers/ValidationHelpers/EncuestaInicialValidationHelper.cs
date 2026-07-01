using AppLogic.Dtos.EncuestaInicial;
using AppLogic.Constants;
using AppLogic.Utilities;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Helpers.ValidationHelpers
{
    public static class EncuestaInicialValidationHelper
    {
        private static readonly int[] OpcionesEms = [0, 2, 3, 4];
        private static readonly int[] CompartidoConCatalogo = [1, 2, 3, 4, 5];
        private static readonly int[] FormacionTutoresCatalogo = [1, 2, 3, 4, 5, 6, 7];
        private static readonly long[] UltimoAnioSecundariaCatalogo = [1, 2];
        private static readonly long[] EstadoEducacionSuperiorPreviaCatalogo = [1, 2, 3];
        private static readonly long[] NivelDecisionCatalogo = [1, 2];
        private static readonly long[] ValoracionesCatalogo = [1, 2, 3, 4, 5];
        private static readonly long[] TipoJornadaCatalogo = [1, 2];

        private sealed record DatosAcademicosEncuesta(
            long? CodigoInstitucionBac,
            string? NombreInstitucion,
            long? CodigoTitulo,
            long ValorEncuesta,
            long UltimoAnioSexto);

        private sealed record TituloAnioResuelto(long? CodigoTitulo, long UltimoAnioSexto);
        private sealed record AnioBachillerResuelto(long ValorEncuesta, long UltimoAnioSexto);

        public static OperationResult<bool> ValidarConsistenciaParcial(
            IUnitOfWork uow,
            DtoGuardarEncuestaInicialRequest request,
            string methodName)
        {
            if (request == null)
            {
                return OperationResult<bool>.IsFailed("INS_EI_02", methodName, "Request invalido.", 400);
            }

            var validacionProducto = ValidarProductoBachillerato(uow, request, methodName);
            if (!validacionProducto.Success)
                return validacionProducto;

            var validacionRangos = ValidarCatalogosRequest(uow, request, methodName);
            if (!validacionRangos.Success)
                return validacionRangos;

            var validacionValoraciones = ValidarValoracionParcial(request, methodName);
            if (!validacionValoraciones.Success)
                return validacionValoraciones;

            var validacionInstitucion = ValidarInstitucionTitulo(uow, request, methodName);
            if (!validacionInstitucion.Success)
                return validacionInstitucion;

            var validacionListas = ValidarListasHijas(uow, request, methodName);
            if (!validacionListas.Success)
                return validacionListas;

            return ValidarCondicionesRequest(uow, request, methodName);
        }

        private static OperationResult<bool> ValidarProductoBachillerato(
            IUnitOfWork uow,
            DtoGuardarEncuestaInicialRequest request,
            string methodName)
        {
            if (!request.CarreraId.HasValue)
                return OperationResult<bool>.Ok(true, methodName);

            if (!uow.Productos.EsProductoValidoParaInteres(request.CarreraId.Value))
                return OperationResult<bool>.IsFailed("INS_EI_03", methodName, "El producto indicado es invalido.", 400);

            var producto = uow.Productos.GetByKey(request.CarreraId.Value);
            if (producto == null)
                return OperationResult<bool>.IsFailed("INS_EI_03", methodName, "El producto indicado es invalido.", 400);

            var anioBachillerato = request.AnioBachillerato.HasValue
                ? ResolverAnioBachiller(uow, request.AnioBachillerato.Value)
                : null;

            if (producto.IdNivelProducto == 1 && anioBachillerato?.UltimoAnioSexto == 4)
                return OperationResult<bool>.IsFailed(
                    "INS_EI_04",
                    methodName,
                    "Para carreras universitarias, el bachillerato indicado debe ser quinto o sexto anio.",
                    400);

            return OperationResult<bool>.Ok(true, methodName);
        }

        private static OperationResult<bool> ValidarCatalogosRequest(
            IUnitOfWork uow,
            DtoGuardarEncuestaInicialRequest request,
            string methodName)
        {
            if (request.ComienzoId is <= 0)
                return OperationResult<bool>.IsFailed("INS_EI_05", methodName, "Proceso invalido.", 400);
            if (request.AnioBachillerato.HasValue && !AnioBachillerCatalogado(uow, request.AnioBachillerato.Value))
                return OperationResult<bool>.IsFailed("INS_EI_06", methodName, "Ultimo anio de bachillerato invalido.", 400);
            if (ValorNoPermitido(request.NivelFormacionMadreTutorId, FormacionTutoresCatalogo))
                return OperationResult<bool>.IsFailed("INS_EI_07", methodName, "Instruccion madre invalida.", 400);
            if (ValorNoPermitido(request.NivelFormacionPadreTutorId, FormacionTutoresCatalogo))
                return OperationResult<bool>.IsFailed("INS_EI_08", methodName, "Instruccion padre invalida.", 400);
            if (ValorNoPermitido(request.AnioDecisionCarreraId, OpcionesEms))
                return OperationResult<bool>.IsFailed("INS_EI_09", methodName, "Decision de carrera invalida.", 400);
            if (ValorNoPermitido(request.AnioDecisionOrtId, OpcionesEms))
                return OperationResult<bool>.IsFailed("INS_EI_10", methodName, "Decision de universidad invalida.", 400);
            if (ValorNoPermitido(request.ApoyoDecisionId, CompartidoConCatalogo))
                return OperationResult<bool>.IsFailed("INS_EI_11", methodName, "Con quien compartio la decision invalido.", 400);
            if (ValorNoPermitido(request.UbicacionUltimoAnioSecundariaId, UltimoAnioSecundariaCatalogo))
                return OperationResult<bool>.IsFailed("INS_EI_14", methodName, "Ultimo anio de secundaria invalido.", 400);
            if (ValorNoPermitido(request.EstadoEducacionSuperiorPreviaId, EstadoEducacionSuperiorPreviaCatalogo))
                return OperationResult<bool>.IsFailed("INS_EI_50", methodName, "Educacion superior previa invalida.", 400);
            if (ValorNoPermitido(request.NivelDecisionId, NivelDecisionCatalogo))
                return OperationResult<bool>.IsFailed("INS_EI_15", methodName, "Nivel de decision invalido.", 400);
            if (ValorNoPermitido(request.TipoJornadaId, TipoJornadaCatalogo))
                return OperationResult<bool>.IsFailed("INS_EI_48", methodName, "Tipo jornada invalido.", 400);
            if (request.OrientacionBachilleratoId.HasValue && request.OrientacionBachilleratoId <= 0)
                return OperationResult<bool>.IsFailed("INS_EI_21", methodName, "Titulo invalido.", 400);
            if (request.OrientacionBachilleratoId.HasValue && !TituloCatalogado(uow, request.OrientacionBachilleratoId.Value))
                return OperationResult<bool>.IsFailed("INS_EI_22", methodName, "El titulo indicado es invalido.", 400);

            return OperationResult<bool>.Ok(true, methodName);
        }

        private static OperationResult<bool> ValidarInstitucionTitulo(
            IUnitOfWork uow,
            DtoGuardarEncuestaInicialRequest request,
            string methodName)
        {
            if (request.InstitucionSecundariaId.HasValue && request.InstitucionSecundariaId <= 0)
                return OperationResult<bool>.IsFailed("INS_EI_19", methodName, "Institucion invalida.", 400);
            if (request.InstitucionSecundariaId.HasValue && uow.Empresas.GetByKey(request.InstitucionSecundariaId.Value) == null)
                return OperationResult<bool>.IsFailed("INS_EI_20", methodName, "La institucion indicada es invalida.", 400);

            return OperationResult<bool>.Ok(true, methodName);
        }

        public static OperationResult<bool> ResolverCompletitud(
            IUnitOfWork uow,
            EncuestaIniAdmision encuesta,
            Persona persona,
            long codigoPersona,
            string methodName)
        {
            if (!TieneCamposMinimosParaCompletar(encuesta))
            {
                return OperationResult<bool>.Ok(false, methodName);
            }

            var ultimoAnioResult = LeerLong(encuesta.UltimoAnioSextoEncuestaIni);
            var instruccionMadreResult = LeerInt(encuesta.InstruccionMadreEncuestaIni);
            var instruccionPadreResult = LeerInt(encuesta.InstruccionPadreEncuestaIni);
            if (!ultimoAnioResult.HasValue || !instruccionMadreResult.HasValue || !instruccionPadreResult.HasValue)
            {
                return OperationResult<bool>.Ok(false, methodName);
            }

            var anioBachillerato = ResolverAnioBachiller(uow, ultimoAnioResult.Value);
            if (anioBachillerato == null)
            {
                return OperationResult<bool>.Ok(false, methodName);
            }

            var producto = uow.Productos.GetByKey(encuesta.IdProducto!.Value);
            if (producto == null || !uow.Productos.EsProductoValidoParaInteres(encuesta.IdProducto.Value))
            {
                return OperationResult<bool>.IsFailed("INS_EI_28", methodName, "El producto indicado es invalido.", 400);
            }

            if (producto.IdNivelProducto == 1 && anioBachillerato.UltimoAnioSexto == 4)
            {
                return OperationResult<bool>.IsFailed(
                    "INS_EI_29",
                    methodName,
                    "Para carreras universitarias, el bachillerato indicado debe ser quinto o sexto anio.",
                    400);
            }

            var datosAcademicos = ResolverDatosAcademicosEncuesta(
                uow,
                encuesta,
                anioBachillerato.ValorEncuesta,
                anioBachillerato.UltimoAnioSexto,
                methodName);
            if (!datosAcademicos.Success)
            {
                return OperationResult<bool>.IsFailed(
                    datosAcademicos.ErrorCode,
                    methodName,
                    datosAcademicos.Message,
                    datosAcademicos.HttpCode);
            }

            if (datosAcademicos.Data == null)
            {
                return OperationResult<bool>.Ok(false, methodName);
            }

            encuesta.CodigoInstitucionBac = datosAcademicos.Data!.CodigoInstitucionBac;
            encuesta.NombreInstSecEncuestaIni = datosAcademicos.Data.NombreInstitucion;
            encuesta.CodigoTitulo = datosAcademicos.Data.CodigoTitulo;
            encuesta.UltimoAnioSextoEncuestaIni = datosAcademicos.Data.ValorEncuesta.ToString();

            if (!CompletoEducacionYListas(uow, encuesta, codigoPersona, anioBachillerato.UltimoAnioSexto))
            {
                return OperationResult<bool>.Ok(false, methodName);
            }

            if (!CompletoValoracionesEInstruccion(encuesta, instruccionMadreResult.Value, instruccionPadreResult.Value))
            {
                return OperationResult<bool>.Ok(false, methodName);
            }

            return OperationResult<bool>.Ok(true, methodName);
        }

        private static bool CompletoEducacionYListas(
            IUnitOfWork uow,
            EncuestaIniAdmision encuesta,
            long codigoPersona,
            long ultimoAnioSexto)
        {
            if (ultimoAnioSexto == 6 && string.IsNullOrWhiteSpace(encuesta.TieneEducacionSuperiorEncuestaIni))
                return false;
            if (encuesta.InforOtrasAntesEncuestaIni == CommonConstants.Booleanos.Si
                && uow.EmpresaConsideradaAdmisions.GetByPersona(codigoPersona).Count == 0)
                return false;
            if (uow.MotivoEleccionAdmisions.GetByPersona(codigoPersona).Count == 0)
                return false;
            if (encuesta.PublicidadOrtEncuestaIni == CommonConstants.Booleanos.Si
                && uow.PublicidadEleccionAdmisions.GetByPersona(codigoPersona).Count == 0)
                return false;
            if (encuesta.TieneEducacionSuperiorEncuestaIni == CommonConstants.Booleanos.Si
                && uow.EducacionSuperiorAdmisions.GetByPersona(codigoPersona).Count == 0)
                return false;

            return true;
        }

        private static bool CompletoValoracionesEInstruccion(
            EncuestaIniAdmision encuesta,
            int instruccionMadre,
            int instruccionPadre)
        {
            if (encuesta.AsesoramientoOrtEncuestaIni == CommonConstants.Booleanos.Si
                && !encuesta.ValoracionAsesoramientoOrtEncuestaIni.HasValue)
                return false;
            if (encuesta.VistaSitioWebOrtEncuestaIni == CommonConstants.Booleanos.Si
                && !encuesta.ValoracionSitioWebOrtEncuestaIni.HasValue)
                return false;
            if (encuesta.VistaInstalacionesOrtEncuestaIni == CommonConstants.Booleanos.Si
                && !encuesta.ValoracionInstalacionesOrtEncuestaIni.HasValue)
                return false;
            if (EsInstruccionAlta(instruccionMadre) && string.IsNullOrWhiteSpace(encuesta.InstruccionMadreOrtEncuestaIni))
                return false;
            if (EsInstruccionAlta(instruccionPadre) && string.IsNullOrWhiteSpace(encuesta.InstruccionPadreOrtEncuestaIni))
                return false;

            return true;
        }

        private static bool EsInstruccionAlta(int valor) => valor is 5 or 6;

        public static OperationResult<long> ObtenerIdComienzoValido(
            IUnitOfWork uow,
            long idProducto,
            long idProceso,
            string methodName)
        {
            var proceso = uow.Procesos.GetProcesosHabilitadosPorProducto(idProducto)
                .FirstOrDefault(p => p.IdProceso == idProceso);
            if (proceso == null)
            {
                return OperationResult<long>.IsFailed(
                    "INS_EI_34",
                    methodName,
                    "No existe un proceso habilitado para el producto indicado.",
                    404);
            }

            var idComienzo = uow.ProcesoComienzos.GetComienzoActivoPorProcesoOProducto(idProducto, idProceso);
            if (!idComienzo.HasValue || idComienzo.Value == 0)
            {
                return OperationResult<long>.IsFailed(
                    "INS_EI_35",
                    methodName,
                    $"No existe comienzo activo, producto:{idProducto} proceso:{idProceso}.",
                    400);
            }

            return OperationResult<long>.Ok(idComienzo.Value, methodName);
        }

        private static OperationResult<bool> ValidarValoracionParcial(DtoGuardarEncuestaInicialRequest request, string methodName)
        {
            if (request.ValoracionAsesoramientoOrtId.HasValue && !ValoracionesCatalogo.Contains(request.ValoracionAsesoramientoOrtId.Value))
                return OperationResult<bool>.IsFailed("INS_EI_16", methodName, "Valoracion de asesoramiento invalida.", 400);
            if (request.ValoracionSitioWebOrtId.HasValue && !ValoracionesCatalogo.Contains(request.ValoracionSitioWebOrtId.Value))
                return OperationResult<bool>.IsFailed("INS_EI_17", methodName, "Valoracion del sitio web invalida.", 400);
            if (request.ValoracionInstalacionesOrtId.HasValue && !ValoracionesCatalogo.Contains(request.ValoracionInstalacionesOrtId.Value))
                return OperationResult<bool>.IsFailed("INS_EI_18", methodName, "Valoracion de instalaciones invalida.", 400);

            return OperationResult<bool>.Ok(true, methodName);
        }

        private static OperationResult<bool> ValidarListasHijas(
            IUnitOfWork uow,
            DtoGuardarEncuestaInicialRequest request,
            string methodName)
        {
            var validacionUniversidades = ValidarEmpresasEncuesta(uow, request.UniversidadConsideradaIds, methodName);
            if (!validacionUniversidades.Success)
                return validacionUniversidades;

            var validacionEducacion = ValidarEmpresasEncuesta(uow, request.UniversidadEducacionSuperiorIds, methodName);
            if (!validacionEducacion.Success)
                return validacionEducacion;

            if (request.MotivoEleccionOrtIds is { Count: > 0 })
            {
                var motivosCatalogo = uow.MotivoOpcionesAdmisions.GetAll();
                if (request.MotivoEleccionOrtIds.Any(motivoId =>
                    motivoId <= 0 || !motivosCatalogo.Any(m => m.IdMotivo == motivoId)))
                {
                    return OperationResult<bool>.IsFailed("INS_EI_23", methodName, "Motivo de eleccion invalido.", 400);
                }
            }

            if (request.PublicidadOrtIds is { Count: > 0 })
            {
                var publicidadesCatalogo = uow.PublicidadOpcionesAdmisions.GetAll();
                if (request.PublicidadOrtIds.Any(publicidadId =>
                    publicidadId <= 0 || !publicidadesCatalogo.Any(p => p.IdPublicidad == publicidadId)))
                {
                    return OperationResult<bool>.IsFailed("INS_EI_24", methodName, "Publicidad seleccionada invalida.", 400);
                }
            }

            return OperationResult<bool>.Ok(true, methodName);
        }

        private static OperationResult<bool> ValidarEmpresasEncuesta(
            IUnitOfWork uow,
            List<long>? empresas,
            string methodName)
        {
            if (empresas == null)
            {
                return OperationResult<bool>.Ok(true, methodName);
            }

            var universidadesCatalogo = uow.Empresas.GetUniversidades();
            foreach (var empresaId in empresas)
            {
                if (empresaId <= 0)
                {
                    return OperationResult<bool>.IsFailed("INS_EI_25", methodName, "Universidad seleccionada invalida.", 400);
                }

                if (!universidadesCatalogo.Any(u => u.CodigoEmpresa == empresaId))
                {
                    return OperationResult<bool>.IsFailed("INS_EI_27", methodName, "Universidad seleccionada invalida.", 400);
                }
            }

            return OperationResult<bool>.Ok(true, methodName);
        }

        private static OperationResult<bool> ValidarCondicionesRequest(
            IUnitOfWork uow,
            DtoGuardarEncuestaInicialRequest request,
            string methodName)
        {
            var anioBachillerato = request.AnioBachillerato.HasValue
                ? ResolverAnioBachiller(uow, request.AnioBachillerato.Value)
                : null;

            if (request.SeInformoEnOtrasUniversidades == true && request.UniversidadConsideradaIds is { Count: 0 })
                return OperationResult<bool>.IsFailed("INS_EI_39", methodName, "Debe indicar universidades consideradas.", 400);
            if (request.EstadoEducacionSuperiorPreviaId is 1 or 2 && request.UniversidadEducacionSuperiorIds is { Count: 0 })
                return OperationResult<bool>.IsFailed("INS_EI_40", methodName, "Debe indicar universidades de educacion superior.", 400);
            if (anioBachillerato?.UltimoAnioSexto == 6 && !request.OrientacionBachilleratoId.HasValue)
                return OperationResult<bool>.IsFailed("INS_EI_41", methodName, "Debe indicar orientacion de bachillerato.", 400);
            if (request.TuvoAsesoramientoOrt == true && !request.ValoracionAsesoramientoOrtId.HasValue)
                return OperationResult<bool>.IsFailed("INS_EI_42", methodName, "Debe indicar valoracion de asesoramiento.", 400);
            if (request.VisitoSitioWebOrt == true && !request.ValoracionSitioWebOrtId.HasValue)
                return OperationResult<bool>.IsFailed("INS_EI_43", methodName, "Debe indicar valoracion del sitio web.", 400);
            if (request.VisitoInstalacionesOrt == true && !request.ValoracionInstalacionesOrtId.HasValue)
                return OperationResult<bool>.IsFailed("INS_EI_44", methodName, "Debe indicar valoracion de instalaciones.", 400);
            if (request.RecuerdaPublicidadOrt == true && request.PublicidadOrtIds is { Count: 0 })
                return OperationResult<bool>.IsFailed("INS_EI_45", methodName, "Debe indicar publicidades seleccionadas.", 400);
            if (EsInstruccionAlta(request.NivelFormacionMadreTutorId ?? 0) && !request.MadreTutorEgresadoOrt.HasValue)
                return OperationResult<bool>.IsFailed("INS_EI_46", methodName, "Debe indicar si la madre o tutor obtuvo el titulo en ORT.", 400);
            if (EsInstruccionAlta(request.NivelFormacionPadreTutorId ?? 0) && !request.PadreTutorEgresadoOrt.HasValue)
                return OperationResult<bool>.IsFailed("INS_EI_47", methodName, "Debe indicar si el padre o tutor obtuvo el titulo en ORT.", 400);
            if (request.TrabajaActualmente == true && !request.TipoJornadaId.HasValue)
                return OperationResult<bool>.IsFailed("INS_EI_49", methodName, "Debe indicar el tipo jornada.", 400);

            return OperationResult<bool>.Ok(true, methodName);
        }

        private static bool TieneCamposMinimosParaCompletar(EncuestaIniAdmision encuesta)
        {
            return encuesta.IdProducto.HasValue
                && encuesta.IdProceso.HasValue
                && encuesta.IdComienzo.HasValue
                && !string.IsNullOrWhiteSpace(encuesta.UltimoAnioSextoEncuestaIni)
                && !string.IsNullOrWhiteSpace(encuesta.InstruccionPadreEncuestaIni)
                && !string.IsNullOrWhiteSpace(encuesta.InstruccionMadreEncuestaIni)
                && !string.IsNullOrWhiteSpace(encuesta.DecisionCarreraEncuestaIni)
                && !string.IsNullOrWhiteSpace(encuesta.DecisionUniverEncuestaIni)
                && !string.IsNullOrWhiteSpace(encuesta.InforOtrasAntesEncuestaIni)
                && !string.IsNullOrWhiteSpace(encuesta.InformarEncuestaIni)
                && encuesta.UltimoanioSecundariaEncuestaIni.HasValue
                && encuesta.NivelDecisionEncuestaIni.HasValue
                && !string.IsNullOrWhiteSpace(encuesta.AsesoramientoOrtEncuestaIni)
                && !string.IsNullOrWhiteSpace(encuesta.VistaSitioWebOrtEncuestaIni)
                && !string.IsNullOrWhiteSpace(encuesta.VistaInstalacionesOrtEncuestaIni)
                && !string.IsNullOrWhiteSpace(encuesta.PublicidadOrtEncuestaIni)
                && TieneDecisionCompartida(encuesta);
        }

        private static bool TieneDecisionCompartida(EncuestaIniAdmision encuesta)
        {
            return encuesta.ComparPadresEncuestaIni == CommonConstants.Booleanos.Si
                || encuesta.ComparOtrosEncuestaIni == CommonConstants.Booleanos.Si
                || encuesta.ComparAmigoFamEncuestaIni == CommonConstants.Booleanos.Si
                || encuesta.ComparNadieEncuestaIni == CommonConstants.Booleanos.Si
                || encuesta.ComparAmigoPropEncuestaIni == CommonConstants.Booleanos.Si;
        }

        private static OperationResult<DatosAcademicosEncuesta> ResolverDatosAcademicosEncuesta(
            IUnitOfWork uow,
            EncuestaIniAdmision encuesta,
            long valorEncuesta,
            long ultimoAnioSexto,
            string methodName)
        {
            long? codigoInstitucionBac = encuesta.CodigoInstitucionBac;
            var nombreInstitucion = encuesta.NombreInstSecEncuestaIni;
            long? codigoTitulo = encuesta.CodigoTitulo;

            if (encuesta.UltimoanioSecundariaEncuestaIni == 1)
            {
                if (!codigoInstitucionBac.HasValue || codigoInstitucionBac.Value <= 0)
                {
                    return OperationResult<DatosAcademicosEncuesta>.Ok(null, methodName);
                }

                var institucion = uow.Empresas.GetByKey(codigoInstitucionBac.Value);
                if (institucion == null)
                {
                    return OperationResult<DatosAcademicosEncuesta>.IsFailed(
                        "INS_EI_30",
                        methodName,
                        "La institucion indicada es invalida.",
                        400);
                }

                nombreInstitucion = institucion.Nombre ?? nombreInstitucion;
            }
            else
            {
                codigoInstitucionBac = PersonaConstants.Parametros.ExteriorInstitucionOrt;
                if (ultimoAnioSexto == 6)
                {
                    codigoTitulo = PersonaConstants.Parametros.TituloGenericoSextoExterior;
                }
            }

            var tituloAnio = ResolverTituloYAnio(uow, ultimoAnioSexto, codigoTitulo, methodName);
            if (!tituloAnio.Success)
            {
                return OperationResult<DatosAcademicosEncuesta>.IsFailed(
                    tituloAnio.ErrorCode, methodName, tituloAnio.Message, tituloAnio.HttpCode);
            }
            if (tituloAnio.Data == null)
            {
                return OperationResult<DatosAcademicosEncuesta>.Ok(null, methodName);
            }

            return OperationResult<DatosAcademicosEncuesta>.Ok(
                new DatosAcademicosEncuesta(
                    codigoInstitucionBac,
                    nombreInstitucion,
                    tituloAnio.Data!.CodigoTitulo,
                    valorEncuesta,
                    tituloAnio.Data.UltimoAnioSexto),
                methodName);
        }

        private static OperationResult<TituloAnioResuelto?> ResolverTituloYAnio(
            IUnitOfWork uow,
            long ultimoAnioSexto,
            long? codigoTitulo,
            string methodName)
        {
            if (ultimoAnioSexto == 6)
            {
                if (!codigoTitulo.HasValue || codigoTitulo.Value <= 0)
                {
                    return OperationResult<TituloAnioResuelto?>.Ok(null, methodName);
                }

                var titulo = uow.Titulos.GetByKey(codigoTitulo.Value);
                if (titulo == null)
                {
                    return OperationResult<TituloAnioResuelto?>.IsFailed(
                        "INS_EI_31", methodName, "El titulo indicado es invalido.", 400);
                }

                var anio = ResolverAnioDeTitulo(uow, titulo, ultimoAnioSexto, methodName);
                if (!anio.Success)
                {
                    return OperationResult<TituloAnioResuelto?>.IsFailed(
                        anio.ErrorCode, methodName, anio.Message, anio.HttpCode);
                }

                ultimoAnioSexto = anio.Data;
            }
            else
            {
                codigoTitulo = null;
                var anioBachiller = uow.AnioBachillers.GetAll()
                    .FirstOrDefault(a => EsAnioBachiller(a, ultimoAnioSexto));
                if (anioBachiller == null)
                {
                    return OperationResult<TituloAnioResuelto?>.IsFailed(
                        "INS_EI_33", methodName, "El bachillerato indicado es invalido.", 400);
                }

                ultimoAnioSexto = (long)anioBachiller.IdAnioBachiller;
            }

            return OperationResult<TituloAnioResuelto?>.Ok(
                new TituloAnioResuelto(codigoTitulo, ultimoAnioSexto), methodName);
        }

        private static OperationResult<long> ResolverAnioDeTitulo(
            IUnitOfWork uow,
            Titulo titulo,
            long ultimoAnioSexto,
            string methodName)
        {
            if (!titulo.IdAnioBachiller.HasValue)
            {
                return OperationResult<long>.Ok(ultimoAnioSexto, methodName);
            }

            var anioTitulo = uow.AnioBachillers.GetByKey((long)titulo.IdAnioBachiller.Value);
            if (anioTitulo == null)
            {
                return OperationResult<long>.IsFailed(
                    "INS_EI_32", methodName, "El bachillerato indicado es invalido.", 400);
            }

            return OperationResult<long>.Ok(
                (long)anioTitulo.IdAnioBachiller, methodName);
        }

        private static AnioBachillerResuelto? ResolverAnioBachiller(IUnitOfWork uow, long valor)
        {
            var anio = uow.AnioBachillers.GetAllWithRelated()
                .FirstOrDefault(a => EsAnioBachiller(a, valor));

            return anio == null
                ? null
                : new AnioBachillerResuelto(valor, (long)anio.IdAnioBachiller);
        }

        private static bool EsAnioBachiller(AnioBachiller anio, long valor)
        {
            return anio.IdAnioBachiller == valor
                || anio.CantAniosAnioBachiller == valor;
        }

        private static long? LeerLong(string? valor)
        {
            return long.TryParse(valor, out var resultado) ? resultado : null;
        }

        private static int? LeerInt(string? valor)
        {
            return int.TryParse(valor, out var resultado) ? resultado : null;
        }

        private static bool ValorNoPermitido(int? valor, params int[] permitidos)
        {
            return valor.HasValue && !permitidos.Contains(valor.Value);
        }

        private static bool ValorNoPermitido(long? valor, params long[] permitidos)
        {
            return valor.HasValue && !permitidos.Contains(valor.Value);
        }

        private static bool AnioBachillerCatalogado(IUnitOfWork uow, long ultimoAnio)
        {
            return uow.AnioBachillers.GetAllWithRelated()
                .Any(a => EsAnioBachiller(a, ultimoAnio));
        }

        private static bool TituloCatalogado(IUnitOfWork uow, long codigoTitulo)
        {
            return uow.AnioBachillers.GetAllWithRelated()
                .SelectMany(a => a.Titulos)
                .Any(t => t.CodigoTitulo == codigoTitulo);
        }
    }
}

