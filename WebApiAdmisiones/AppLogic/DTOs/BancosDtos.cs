namespace AppLogic.DTOs
{
    public class BancosResponseDto
    {
        public List<BancoDto> Bancos { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class BancoDto
    {
        public long IdBanco { get; set; }
        public string? NombreBanco { get; set; }
        public string? Codigo { get; set; }
        public bool Activo { get; set; }
    }
}
