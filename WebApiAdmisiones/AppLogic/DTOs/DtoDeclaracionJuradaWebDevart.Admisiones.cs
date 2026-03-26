using System.Diagnostics.CodeAnalysis;

namespace AppLogic.DevartDTOs
{
    [ExcludeFromCodeCoverage]
    public partial class DtoDeclaracionJuradaWebDevart
    {
        public string? NombreBachillerato { get; set; }
        public DtoTipoViviendaDevart? ObjTipoVivienda { get; set; }
        public DtoPruebaDevart? ObjPrueba { get; set; }
    }
}
