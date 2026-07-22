using AppLogic.Inscripciones.Encuesta.Dtos;
using AppLogic.Inscripciones.Encuesta.Rules;
using BusinessLogic.Entities;
using BusinessLogic.IDevartRepositories;
using Utilities;

namespace AppLogic.Inscripciones.Encuesta.Validators;

public static class EncuestaInicialCatalogValidation
{
    /// <summary>IdNivelProducto que identifica una carrera universitaria (a diferencia de terciarias u otros niveles).</summary>
    private const long NivelProductoUniversitario = 1;

    /// <summary>Cuarto año de bachillerato, identificado según cuál campo resolvió el año: IdAnioBachiller usa la numeración corta (1-6), CantAniosAnioBachiller la cantidad de años acumulados (hasta 12).</summary>
    private const long IdAnioBachillerCuartoAnio = 4;
    private const long CantAniosBachilleratoCuartoAnio = 10;

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
        Producto? producto = null;
        if (request.CarreraId.HasValue)
        {
            if (!uow.Productos.EsProductoValidoParaInteres(request.CarreraId.Value))
                return OperationResult<bool>.IsFailed("INS_EI_03", methodName, "El producto indicado es invalido.", 400);

            producto = uow.Productos.GetByKey(request.CarreraId.Value);
        }

        var procesosDisponibles = uow.VdProcesosDisponibles1y2s;
        if (request.CarreraId.HasValue
            && request.ProcesoId.HasValue
            && procesosDisponibles != null
            && !procesosDisponibles.GetProcesosDisponibles(request.CarreraId.Value).Any(p => p.IdProceso == request.ProcesoId.Value))
        {
            return OperationResult<bool>.IsFailed("INS_EI_34", methodName, "No existe un proceso habilitado para el producto indicado.", 404);
        }

        var aplicaBachillerato = request.CursaSecundariaActualmente != false;
        if (aplicaBachillerato && request.AnioBachillerato.HasValue && ResolverAnioBachiller(uow, request.AnioBachillerato.Value) == null)
            return OperationResult<bool>.IsFailed("INS_EI_06", methodName, "Ultimo anio de bachillerato invalido.", 400);

        if (aplicaBachillerato
            && producto?.IdNivelProducto == NivelProductoUniversitario
            && request.AnioBachillerato.HasValue
            && EsCuartoBachillerato(uow, request.AnioBachillerato.Value))
            return OperationResult<bool>.IsFailed("INS_EI_64", methodName, "Para carreras universitarias, el bachillerato indicado debe ser quinto o sexto año.", 400);

        if (aplicaBachillerato && request.OrientacionBachilleratoId.HasValue && !TituloCatalogado(uow, request.OrientacionBachilleratoId.Value))
            return OperationResult<bool>.IsFailed("INS_EI_22", methodName, "El titulo indicado es invalido.", 400);

        if (request.UbicacionUltimoAnioSecundariaId == EncuestaInicialState.UbicacionUltimoAnioSecundaria.Uruguay)
        {
            if (request.InstitucionSecundariaId is <= 0)
                return OperationResult<bool>.IsFailed("INS_EI_19", methodName, "Institucion invalida.", 400);
            if (request.InstitucionSecundariaId.HasValue && uow.Empresas.GetByKey(request.InstitucionSecundariaId.Value) == null)
                return OperationResult<bool>.IsFailed("INS_EI_20", methodName, "La institucion indicada es invalida.", 400);
        }

        var empresas = ValidarEmpresas(uow, request.UniversidadConsideradaIds, request.UniversidadConsideradaOtros, methodName);
        if (!empresas.Success)
            return empresas;

        if (request.EstadoEducacionSuperiorPreviaId == EncuestaInicialState.EstadoEducacionSuperiorPrevia.Uruguay)
        {
            empresas = ValidarEmpresas(uow, request.UniversidadEducacionSuperiorIds, request.UniversidadEducacionSuperiorOtros, methodName);
            if (!empresas.Success)
                return empresas;
        }

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
        if (request.RecursaAnioBachillerato == true
            && (!request.VecesRecursaAnioBachillerato.HasValue || request.VecesRecursaAnioBachillerato.Value < 1))
            return OperationResult<bool>.IsFailed("INS_EI_52", methodName, "Debe indicar una cantidad valida de veces que recursa el anio de bachillerato.", 400);

        if (request.EstadoEducacionSuperiorPreviaId == EncuestaInicialState.EstadoEducacionSuperiorPrevia.Uruguay
            && (request.UniversidadEducacionSuperiorIds == null || request.UniversidadEducacionSuperiorIds.Count == 0)
            && !TieneOtros(request.UniversidadEducacionSuperiorOtros))
            return OperationResult<bool>.IsFailed("INS_EI_63", methodName, "Debe indicar al menos una universidad de educacion superior.", 400);
        if (request.EstadoEducacionSuperiorPreviaId != EncuestaInicialState.EstadoEducacionSuperiorPrevia.Uruguay
            && (request.UniversidadEducacionSuperiorIds?.Count > 0 || TieneOtros(request.UniversidadEducacionSuperiorOtros)))
            return OperationResult<bool>.IsFailed("INS_EI_25", methodName, "Universidad seleccionada invalida.", 400);

        if (request.TuvoAsesoramientoOrt == true && !request.ValoracionAsesoramientoOrtId.HasValue)
            return OperationResult<bool>.IsFailed("INS_EI_57", methodName, "Debe indicar valoracion de asesoramiento ORT.", 400);
        if (request.VisitoSitioWebOrt == true && !request.ValoracionSitioWebOrtId.HasValue)
            return OperationResult<bool>.IsFailed("INS_EI_58", methodName, "Debe indicar valoracion del sitio web ORT.", 400);
        if (request.VisitoInstalacionesOrt == true && !request.ValoracionInstalacionesOrtId.HasValue)
            return OperationResult<bool>.IsFailed("INS_EI_59", methodName, "Debe indicar valoracion de instalaciones ORT.", 400);

        if (request.TrabajaActualmente == true && !request.TipoJornadaId.HasValue)
            return OperationResult<bool>.IsFailed("INS_EI_60", methodName, "Debe indicar tipo de jornada.", 400);

        if (EncuestaInicialState.EsInstruccionAlta(request.NivelFormacionPadreTutorId) && !request.PadreTutorEgresadoOrt.HasValue)
            return OperationResult<bool>.IsFailed("INS_EI_61", methodName, "Debe indicar si padre/tutor es egresado ORT.", 400);
        if (EncuestaInicialState.EsInstruccionAlta(request.NivelFormacionMadreTutorId) && !request.MadreTutorEgresadoOrt.HasValue)
            return OperationResult<bool>.IsFailed("INS_EI_62", methodName, "Debe indicar si madre/tutor es egresada ORT.", 400);

        return OperationResult<bool>.Ok(true, methodName);
    }

    private static OperationResult<bool> ValidarEmpresas(
        IUnitOfWork uow,
        List<long>? empresas,
        List<string>? otros,
        string methodName)
    {
        if ((empresas == null || empresas.Count == 0) && !TieneOtros(otros))
            return OperationResult<bool>.Ok(true, methodName);

        var universidades = uow.Empresas.GetUniversidades();
        var tieneOtroSeleccionado = empresas?.Contains(0) == true;
        var tieneNombreOtro = TieneOtros(otros);

        if (empresas?.Any(id => id < 0) == true)
            return OperationResult<bool>.IsFailed("INS_EI_25", methodName, "Universidad seleccionada invalida.", 400);
        if (tieneOtroSeleccionado != tieneNombreOtro)
            return OperationResult<bool>.IsFailed("INS_EI_25", methodName, "Universidad seleccionada invalida.", 400);
        if (empresas?.Where(id => id > 0).Any(id => !universidades.Any(u => u.CodigoEmpresa == id)) == true)
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

    private static bool EsCuartoBachillerato(IUnitOfWork uow, long value)
    {
        var anio = ResolverAnioBachiller(uow, value);
        return value is IdAnioBachillerCuartoAnio or CantAniosBachilleratoCuartoAnio
            || anio?.IdAnioBachiller == IdAnioBachillerCuartoAnio
            || anio?.CantAniosAnioBachiller == CantAniosBachilleratoCuartoAnio;
    }

    private static bool TieneOtros(List<string>? otros)
        => otros?.Any(o => !string.IsNullOrWhiteSpace(o)) == true;
}
