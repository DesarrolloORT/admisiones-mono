using AppLogic.IServices.Tivenos;

namespace AppLogic.DTOs
{
    public sealed class TivenosAltaInteresRequest
    {
        public long CodigoPersona { get; init; }
        public long IdProducto { get; init; }
        public long IdProceso { get; init; }
        public TivenosAltaInteresOperacion Operacion { get; init; } = new();
    }
}
