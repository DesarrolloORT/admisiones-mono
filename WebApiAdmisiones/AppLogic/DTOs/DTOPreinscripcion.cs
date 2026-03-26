using AppLogic.DevartDTOs;
using System.Diagnostics.CodeAnalysis;

namespace AppLogic.DTOs
{
    [ExcludeFromCodeCoverage]
    public class DtoDatosPreInscripcion
    {
        public long IdProceso { get; set; }
        public string? NombreProceso { get; set; }
        public long IdComienzo { get; set; }
        public string? NombreComienzo { get; set; }
        public long IdProducto { get; set; }
        public string? NombreExtensoProducto { get; set; }
        public DtoTurnoDevart? ObjTurno { get; set; }
        public string? TipoInscripcion { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public DtoOfertaDevart? ObjOferta { get; set; }
    }
}
