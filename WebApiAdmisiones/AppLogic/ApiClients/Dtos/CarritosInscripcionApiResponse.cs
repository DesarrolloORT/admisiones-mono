using System.Collections.Generic;

namespace AppLogic.ApiClients.Dtos
{
    public class CarritosInscripcionApiResponse
    {
        public List<CarritoSeniaApiDto> Carritos { get; set; } = new();
        public EstadoCuentaApiDto? EstadoCuenta { get; set; }
    }
}
