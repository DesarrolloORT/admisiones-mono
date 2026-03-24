using AppLogic.DevartDTOs;

namespace AppLogic.DTOs
{
    public class DTODatosPreInscripcion
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
