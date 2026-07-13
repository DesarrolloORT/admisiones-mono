namespace AppLogic.ApiClients.Dtos
{
    /// <summary>
    /// DTO para turno (usado en confirmar preinscripción).
    /// </summary>
    public class DtoTurno
    {
        public long IdTurno { get; set; }
        public string? NombreTurno { get; set; }
    }
}
