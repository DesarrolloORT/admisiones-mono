using System.Collections.Generic;

namespace AppLogic.Inscripciones.Dtos
{
    public class DtoInteresProductoRequest
    {
        public long IdProducto { get; set; }
        public long IdProcesoSeleccionado { get; set; }

        /// <summary>
        /// Ofertas de interes. Para productos de nivel 1 y 2 debe traer exactamente una;
        /// para nivel 3 y 4 puede traer varias.
        /// </summary>
        public List<long> IdsOferta { get; set; } = new();
    }
}
