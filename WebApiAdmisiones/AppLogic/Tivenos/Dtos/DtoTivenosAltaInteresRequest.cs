namespace AppLogic.Tivenos.Dtos
{
    public sealed class DtoTivenosAltaInteresRequest
    {
        public long CodigoPersona { get; init; }
        public long IdProducto { get; init; }
        public long IdProceso { get; init; }
        public TivenosAltaInteresOperacion Operacion { get; init; } = new();
    }
}
