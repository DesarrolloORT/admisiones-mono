using AppLogic.Integrations.Tivenos.Dtos;
using BusinessLogic.Entities;

namespace AppLogic.Integrations.Tivenos.Mapping;

/// <summary>
/// Arma los mensajes que se encolan en T_ENVIO_PARA_TIVENOS.
/// Traducción pura de request a entidad: no toca la base ni decide nada de negocio.
/// </summary>
internal static class TivenosMessageMapper
{
    private const string OrigenAdmisiones = "ADMISIONES";
    private const string StatusNuevo = "Nuevo";

    /// <summary>Alta de interés por producto originada en la selección del sitio de admisiones.</summary>
    internal static EnvioParaTiveno ToProductInterestMessage(DtoTivenosAltaInteresRequest request)
    {
        return new EnvioParaTiveno
        {
            OrigenLlamador = request.Operacion.OrigenLlamador,
            Origen = OrigenAdmisiones,
            TipoProcesoLlamador = request.Operacion.TipoProcesoLlamador,
            Disparador = request.Operacion.Disparador,
            Modulo = "InteresProducto",
            Metodo = "AltaInteresXSeleccionEnSitio",
            Status = StatusNuevo,
            CodigoSape = request.CodigoPersona,
            ProcesoId = request.IdProceso,
            ProductoId = request.IdProducto,
            InteresProdGradoInteresId = 4,
            MotivodesinteresId = null,
            MotivodesinteresNombre = string.Empty,
        };
    }

    /// <summary>
    /// Alta de la persona en el CRM, al registrar su primer interés desde el sitio.
    /// No lleva grado de interés ni motivo de desinterés: el mensaje habla de la persona, no del producto.
    /// </summary>
    internal static EnvioParaTiveno ToSiteRegistrationMessage(DtoTivenosAltaInteresRequest request)
    {
        return new EnvioParaTiveno
        {
            OrigenLlamador = request.Operacion.OrigenLlamador,
            Origen = OrigenAdmisiones,
            TipoProcesoLlamador = request.Operacion.TipoProcesoLlamador,
            Disparador = request.Operacion.Disparador,
            Modulo = "InteresPersona",
            Metodo = "RegistroDesdeSitioAdmisiones",
            Status = StatusNuevo,
            CodigoSape = request.CodigoPersona,
            ProcesoId = request.IdProceso,
            ProductoId = request.IdProducto,
        };
    }

    /// <summary>Alta o modificación de los datos de bachillerato de la persona.</summary>
    internal static EnvioParaTiveno ToHighSchoolMessage(
        DtoTivenosBachilleratoRequest request,
        string tipoProcesoLlamador,
        string disparador,
        string metodo)
    {
        return new EnvioParaTiveno
        {
            Origen = OrigenAdmisiones,
            TipoProcesoLlamador = tipoProcesoLlamador,
            Disparador = disparador,
            Modulo = "Bachillerato",
            Metodo = metodo,
            Status = StatusNuevo,
            CodigoSape = request.CodigoPersona,
            BachilleratoOrientacionId = request.CodigoOrientacion,
        };
    }
}
