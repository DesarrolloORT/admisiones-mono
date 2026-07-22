namespace AppLogic.ApiClients.Dtos;

public class ResumenInscripcionApiDto
{
    public long IdOferta { get; set; }
    public long IdProducto { get; set; }
    public string? Carrera { get; set; }
    public long IdComienzo { get; set; }
    public string? Comienzo { get; set; }
    public long IdTurno { get; set; }
    public string? Turno { get; set; }
}
