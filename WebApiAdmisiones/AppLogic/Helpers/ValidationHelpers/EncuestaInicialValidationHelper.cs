using AppLogic.Constants;
using AppLogic.DTOs;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Helpers.ValidationHelpers
{
    public static class EncuestaInicialValidationHelper
    {
        private const string TrabajaActualmenteSi = "S";
        private const string TrabajaActualmenteNo = "N";

        private sealed record DatosAcademicosEncuesta(
            long? CodigoInstitucionBac,
            string? NombreInstitucion,
            long? CodigoTitulo,
            long UltimoAnioSexto);

        public static OperationResult<bool> ValidarConsistenciaParcial(
            IUnitOfWork uow,
            GuardarEncuestaInicialRequest request,
            string methodName)
        {
            if (request == null)
            {
                return OperationResult<bool>.IsFailed("INS_EI_02", methodName, "Request invalido.", 400);
            }

            if (!EsTrabajaActualmenteONulo(request.TrabajaActualmente))
            {
                return OperationResult<bool>.IsFailed("INS_EI_39", methodName, "Debe indicar si trabaja actualmente.", 400);
            }

            if (request.IdProducto.HasValue)
            {
                if (!uow.Productos.EsProductoValidoParaInteres(request.IdProducto.Value))
                {
                    return OperationResult<bool>.IsFailed("INS_EI_03", methodName, "El producto indicado es invalido.", 400);
                }

                var producto = uow.Productos.GetByKey(request.IdProducto.Value);
                if (producto == null)
                {
                    return OperationResult<bool>.IsFailed("INS_EI_03", methodName, "El producto indicado es invalido.", 400);
                }

                if (producto.IdNivelProducto == 1 && request.UltimoAnioSexto == 4)
                {
                    return OperationResult<bool>.IsFailed(
                        "INS_EI_04",
                        methodName,
                        "Para carreras universitarias, el bachillerato indicado debe ser quinto o sexto anio.",
                        400);
                }
            }

            if (request.IdProceso is <= 0)
                return OperationResult<bool>.IsFailed("INS_EI_05", methodName, "Proceso invalido.", 400);
            if (request.UltimoAnioSexto.HasValue && request.UltimoAnioSexto is < 4 or > 6)
                return OperationResult<bool>.IsFailed("INS_EI_06", methodName, "Ultimo anio de bachillerato invalido.", 400);
            if (request.InstruccionMadre.HasValue && request.InstruccionMadre is < 1 or > 7)
                return OperationResult<bool>.IsFailed("INS_EI_07", methodName, "Instruccion madre invalida.", 400);
            if (request.InstruccionPadre.HasValue && request.InstruccionPadre is < 1 or > 7)
                return OperationResult<bool>.IsFailed("INS_EI_08", methodName, "Instruccion padre invalida.", 400);
            if (request.DecisionCarrera.HasValue && request.DecisionCarrera is not (0 or 2 or 3 or 4))
                return OperationResult<bool>.IsFailed("INS_EI_09", methodName, "Decision de carrera invalida.", 400);
            if (request.DecisionUniversidad.HasValue && request.DecisionUniversidad is not (0 or 2 or 3 or 4))
                return OperationResult<bool>.IsFailed("INS_EI_10", methodName, "Decision de universidad invalida.", 400);
            if (request.CompartidoCon.HasValue && request.CompartidoCon < PersonaConstants.CompartidoCon.Padres)
                return OperationResult<bool>.IsFailed("INS_EI_11", methodName, "Con quien compartio la decision invalido.", 400);
            if (request.CompartidoCon.HasValue && request.CompartidoCon > PersonaConstants.CompartidoCon.Nadie)
                return OperationResult<bool>.IsFailed("INS_EI_11", methodName, "Con quien compartio la decision invalido.", 400);
            if (!EsSiNoONulo(request.InfoOtrasUniversidadesAntes))
                return OperationResult<bool>.IsFailed("INS_EI_12", methodName, "Debe indicar SI o NO para universidades consideradas.", 400);
            if (!EsSiNoONulo(request.InformarEncuesta))
                return OperationResult<bool>.IsFailed("INS_EI_13", methodName, "Debe indicar SI o NO para informar encuesta.", 400);
            if (request.UltimoAnioSecundaria.HasValue && request.UltimoAnioSecundaria is not (1 or 2))
                return OperationResult<bool>.IsFailed("INS_EI_14", methodName, "Ultimo anio de secundaria invalido.", 400);
            if (request.NivelDecision.HasValue && request.NivelDecision is not (1 or 2))
                return OperationResult<bool>.IsFailed("INS_EI_15", methodName, "Nivel de decision invalido.", 400);

            var validacionValoraciones = ValidarValoracionParcial(request, methodName);
            if (!validacionValoraciones.Success)
                return validacionValoraciones;

            if (request.CodigoInstitucionBac.HasValue && request.CodigoInstitucionBac <= 0)
                return OperationResult<bool>.IsFailed("INS_EI_19", methodName, "Institucion invalida.", 400);
            if (request.CodigoInstitucionBac.HasValue && uow.Empresas.GetByKey(request.CodigoInstitucionBac.Value) == null)
                return OperationResult<bool>.IsFailed("INS_EI_20", methodName, "La institucion indicada es invalida.", 400);
            if (request.CodigoTitulo.HasValue && request.CodigoTitulo <= 0)
                return OperationResult<bool>.IsFailed("INS_EI_21", methodName, "Titulo invalido.", 400);
            if (request.CodigoTitulo.HasValue && uow.Titulos.GetByKey(request.CodigoTitulo.Value) == null)
                return OperationResult<bool>.IsFailed("INS_EI_22", methodName, "El titulo indicado es invalido.", 400);

            var validacionListas = ValidarListasHijas(uow, request, methodName);
            if (!validacionListas.Success)
                return validacionListas;

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

            var producto = uow.Productos.GetByKey(encuesta.IdProducto!.Value);
            if (producto == null || !uow.Productos.EsProductoValidoParaInteres(encuesta.IdProducto.Value))
            {
                return OperationResult<bool>.IsFailed("INS_EI_28", methodName, "El producto indicado es invalido.", 400);
            }

            if (producto.IdNivelProducto == 1 && ultimoAnioResult.Value == 4)
            {
                return OperationResult<bool>.IsFailed(
                    "INS_EI_29",
                    methodName,
                    "Para carreras universitarias, el bachillerato indicado debe ser quinto o sexto anio.",
                    400);
            }

            var datosAcademicos = ResolverDatosAcademicosEncuesta(uow, encuesta, ultimoAnioResult.Value, methodName);
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
            encuesta.UltimoAnioSextoEncuestaIni = datosAcademicos.Data.UltimoAnioSexto.ToString();

            if (ultimoAnioResult.Value == 6 && string.IsNullOrWhiteSpace(encuesta.TieneEducacionSuperiorEncuestaIni))
            {
                return OperationResult<bool>.Ok(false, methodName);
            }

            if (encuesta.InforOtrasAntesEncuestaIni == CommonConstants.Booleanos.Si
                && !uow.EmpresaConsideradaAdmisions.GetByPersona(codigoPersona).Any())
            {
                return OperationResult<bool>.Ok(false, methodName);
            }

            if (!uow.MotivoEleccionAdmisions.GetByPersona(codigoPersona).Any())
            {
                return OperationResult<bool>.Ok(false, methodName);
            }

            if (encuesta.PublicidadOrtEncuestaIni == CommonConstants.Booleanos.Si
                && !uow.PublicidadEleccionAdmisions.GetByPersona(codigoPersona).Any())
            {
                return OperationResult<bool>.Ok(false, methodName);
            }

            if (encuesta.TieneEducacionSuperiorEncuestaIni == CommonConstants.Booleanos.Si
                && !uow.EducacionSuperiorAdmisions.GetByPersona(codigoPersona).Any())
            {
                return OperationResult<bool>.Ok(false, methodName);
            }

            if (encuesta.AsesoramientoOrtEncuestaIni == CommonConstants.Booleanos.Si
                && !encuesta.ValoracionAsesoramientoOrtEncuestaIni.HasValue)
            {
                return OperationResult<bool>.Ok(false, methodName);
            }

            if (encuesta.VistaSitioWebOrtEncuestaIni == CommonConstants.Booleanos.Si
                && !encuesta.ValoracionSitioWebOrtEncuestaIni.HasValue)
            {
                return OperationResult<bool>.Ok(false, methodName);
            }

            if (encuesta.VistaInstalacionesOrtEncuestaIni == CommonConstants.Booleanos.Si
                && !encuesta.ValoracionInstalacionesOrtEncuestaIni.HasValue)
            {
                return OperationResult<bool>.Ok(false, methodName);
            }

            if (instruccionMadreResult.Value is 5 or 6 && string.IsNullOrWhiteSpace(encuesta.InstruccionMadreOrtEncuestaIni))
            {
                return OperationResult<bool>.Ok(false, methodName);
            }

            if (instruccionPadreResult.Value is 5 or 6 && string.IsNullOrWhiteSpace(encuesta.InstruccionPadreOrtEncuestaIni))
            {
                return OperationResult<bool>.Ok(false, methodName);
            }

            if (EsPersonaSgi(persona) && !EsTrabajaActualmenteValido(persona.TrabajaActualmente))
            {
                return OperationResult<bool>.Ok(false, methodName);
            }

            return OperationResult<bool>.Ok(true, methodName);
        }

        public static string? NormalizarTrabajaActualmente(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim().ToUpperInvariant();
        }

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

        private static OperationResult<bool> ValidarValoracionParcial(GuardarEncuestaInicialRequest request, string methodName)
        {
            if (request.ValoracionAsesoramientoOrt.HasValue && request.ValoracionAsesoramientoOrt is < 1 or > 5)
                return OperationResult<bool>.IsFailed("INS_EI_16", methodName, "Valoracion de asesoramiento invalida.", 400);
            if (request.ValoracionSitioWeb.HasValue && request.ValoracionSitioWeb is < 1 or > 5)
                return OperationResult<bool>.IsFailed("INS_EI_17", methodName, "Valoracion del sitio web invalida.", 400);
            if (request.ValoracionInstalacionesOrt.HasValue && request.ValoracionInstalacionesOrt is < 1 or > 5)
                return OperationResult<bool>.IsFailed("INS_EI_18", methodName, "Valoracion de instalaciones invalida.", 400);

            return OperationResult<bool>.Ok(true, methodName);
        }

        private static OperationResult<bool> ValidarListasHijas(
            IUnitOfWork uow,
            GuardarEncuestaInicialRequest request,
            string methodName)
        {
            var validacionUniversidades = ValidarEmpresasEncuesta(uow, request.UniversidadesConsideradas, methodName);
            if (!validacionUniversidades.Success)
                return validacionUniversidades;

            var validacionEducacion = ValidarEmpresasEncuesta(uow, request.UniversidadesEducacionSuperior, methodName);
            if (!validacionEducacion.Success)
                return validacionEducacion;

            if (request.OpcionesMotivosSeleccionados != null)
            {
                foreach (var motivo in request.OpcionesMotivosSeleccionados)
                {
                    if (motivo.IdMotivo <= 0 || uow.MotivoOpcionesAdmisions.GetByKey(motivo.IdMotivo) == null)
                    {
                        return OperationResult<bool>.IsFailed("INS_EI_23", methodName, "Motivo de eleccion invalido.", 400);
                    }
                }
            }

            if (request.OpcionesPublicidadSeleccionadas != null)
            {
                foreach (var publicidad in request.OpcionesPublicidadSeleccionadas)
                {
                    if (publicidad.IdPublicidad <= 0 || uow.PublicidadOpcionesAdmisions.GetByKey(publicidad.IdPublicidad) == null)
                    {
                        return OperationResult<bool>.IsFailed("INS_EI_24", methodName, "Publicidad seleccionada invalida.", 400);
                    }
                }
            }

            return OperationResult<bool>.Ok(true, methodName);
        }

        private static OperationResult<bool> ValidarEmpresasEncuesta(
            IUnitOfWork uow,
            List<EncuestaEmpresaRequest>? empresas,
            string methodName)
        {
            if (empresas == null)
            {
                return OperationResult<bool>.Ok(true, methodName);
            }

            foreach (var empresa in empresas)
            {
                if (empresa.CodigoEmpresa < 0)
                {
                    return OperationResult<bool>.IsFailed("INS_EI_25", methodName, "Universidad seleccionada invalida.", 400);
                }

                if (empresa.CodigoEmpresa == 0)
                {
                    if (string.IsNullOrWhiteSpace(empresa.Nombre) || string.Equals(empresa.Nombre.Trim(), "Otro", StringComparison.OrdinalIgnoreCase))
                    {
                        return OperationResult<bool>.IsFailed("INS_EI_26", methodName, "El nombre de la universidad no puede ser vacio.", 400);
                    }

                    continue;
                }

                if (uow.Empresas.GetByKey(empresa.CodigoEmpresa) == null)
                {
                    return OperationResult<bool>.IsFailed("INS_EI_27", methodName, "Universidad seleccionada invalida.", 400);
                }
            }

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
            long ultimoAnioSexto,
            string methodName)
        {
            long? codigoInstitucionBac = encuesta.CodigoInstitucionBac;
            var nombreInstitucion = encuesta.NombreInstSecEncuestaIni;
            long? codigoTitulo = encuesta.CodigoTitulo;

            if (encuesta.UltimoanioSecundariaEncuestaIni == true)
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

            if (ultimoAnioSexto == 6)
            {
                if (!codigoTitulo.HasValue || codigoTitulo.Value <= 0)
                {
                    return OperationResult<DatosAcademicosEncuesta>.Ok(null, methodName);
                }

                var titulo = uow.Titulos.GetByKey(codigoTitulo.Value);
                if (titulo == null)
                {
                    return OperationResult<DatosAcademicosEncuesta>.IsFailed(
                        "INS_EI_31",
                        methodName,
                        "El titulo indicado es invalido.",
                        400);
                }

                if (titulo.IdAnioBachiller.HasValue)
                {
                    var anioTitulo = uow.AnioBachillers.GetByKey((long)titulo.IdAnioBachiller.Value);
                    if (anioTitulo == null)
                    {
                        return OperationResult<DatosAcademicosEncuesta>.IsFailed(
                            "INS_EI_32",
                            methodName,
                            "El bachillerato indicado es invalido.",
                            400);
                    }

                    ultimoAnioSexto = (long)(anioTitulo.CantAniosAnioBachiller ?? ultimoAnioSexto);
                }
            }
            else
            {
                codigoTitulo = null;
                var anioBachiller = uow.AnioBachillers.GetAll()
                    .FirstOrDefault(a => a.CantAniosAnioBachiller == ultimoAnioSexto);
                if (anioBachiller == null)
                {
                    return OperationResult<DatosAcademicosEncuesta>.IsFailed(
                        "INS_EI_33",
                        methodName,
                        "El bachillerato indicado es invalido.",
                        400);
                }

                ultimoAnioSexto = (long)(anioBachiller.CantAniosAnioBachiller ?? ultimoAnioSexto);
            }

            return OperationResult<DatosAcademicosEncuesta>.Ok(
                new DatosAcademicosEncuesta(codigoInstitucionBac, nombreInstitucion, codigoTitulo, ultimoAnioSexto),
                methodName);
        }

        private static long? LeerLong(string? valor)
        {
            return long.TryParse(valor, out var resultado) ? resultado : null;
        }

        private static int? LeerInt(string? valor)
        {
            return int.TryParse(valor, out var resultado) ? resultado : null;
        }

        private static bool EsSiNoONulo(string? valor)
        {
            var normalizado = NormalizarSiNo(valor);
            return normalizado == null
                || normalizado == CommonConstants.Booleanos.Si
                || normalizado == CommonConstants.Booleanos.No;
        }

        private static bool EsTrabajaActualmenteONulo(string? valor)
        {
            var normalizado = NormalizarTrabajaActualmente(valor);
            return normalizado == null || EsTrabajaActualmenteValido(normalizado);
        }

        private static bool EsTrabajaActualmenteValido(string? valor)
        {
            var normalizado = NormalizarTrabajaActualmente(valor);
            return normalizado == TrabajaActualmenteSi || normalizado == TrabajaActualmenteNo;
        }

        private static bool EsPersonaSgi(Persona persona)
        {
            return string.Equals(persona.TipoPersona, PersonaConstants.TipoPersonaSgi, StringComparison.OrdinalIgnoreCase);
        }

        private static string? NormalizarSiNo(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim().ToUpperInvariant();
        }
    }
}
