using AppLogic.DevartDTOs;
using System.Diagnostics.CodeAnalysis;

namespace AppLogic.Becas.Dtos;

[ExcludeFromCodeCoverage]
public class DtoDeclaracionJuradaAdmisiones
{
    public string? SubEstado { get; set; }
    public long IdProducto { get; set; }
    public string? NombreExtensoProducto { get; set; }
    public string? NombreCentroCosto { get; set; }
    public long IdInscriptoPrueba { get; set; }
    public long IdPrueba { get; set; }
    public long IdTipoBeca { get; set; }
    public DateTime? FechaLimiteGuiaPrueba { get; set; }
    public string? HoraLimiteGuiaPrueba { get; set; }
    public DateTime? FechaDifusionWebPrueba { get; set; }
    public string? AliasTipoBeca { get; set; }
    public long IdNivelProducto { get; set; }
    public string HabilitadoSitioORT { get; set; } = "SI";
    public string? Detalle { get; set; }
    public string? Nombre { get; set; }
    public DateTime? FechaEntregaDJPrueba { get; set; }
    public string? HoraEnrtegaDJPrueba { get; set; }
}

[ExcludeFromCodeCoverage]
public static class DeclaracionJuradaAdmisionesMapper
{
    public static DtoDeclaracionJuradaAdmisiones ToAdmisionesDto(
        this DtoDeclaracionJuradaWebDevart declaracion,
        DtoPruebaDevart? prueba)
    {
        return new DtoDeclaracionJuradaAdmisiones
        {
            SubEstado = declaracion.Subestado,
            IdProducto = declaracion.Producto?.IdProducto ?? declaracion.IdProducto,
            NombreExtensoProducto = declaracion.Producto?.NombreExtensoProducto,
            NombreCentroCosto = declaracion.Producto?.IdCentroCostos,
            IdInscriptoPrueba = (long)declaracion.IdInscriptoPrueba,
            IdPrueba = prueba?.IdPrueba ?? 0,
            IdTipoBeca = declaracion.TipoDescuento?.IdTipoDescuento ?? declaracion.IdTipoDescuento,
            FechaLimiteGuiaPrueba = prueba?.FechaLimiteGuiaPrueba,
            HoraLimiteGuiaPrueba = prueba?.HoraLimiteGuiaPrueba,
            FechaDifusionWebPrueba = prueba?.FechaDifusionwebPrueba,
            AliasTipoBeca = declaracion.TipoDescuento?.AliasTipoDescuento,
            IdNivelProducto = declaracion.Producto?.IdNivelProducto ?? 0,
            Detalle = declaracion.TipoDescuento?.DetalleTipoDescuento,
            Nombre = declaracion.TipoDescuento?.NombreTipoDescuento,
            FechaEntregaDJPrueba = prueba?.FechaEntregaDjPrueba,
            HoraEnrtegaDJPrueba = prueba?.HoraEntregaDjPrueba
        };
    }
}
