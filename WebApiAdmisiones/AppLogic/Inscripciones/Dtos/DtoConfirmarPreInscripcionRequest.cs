using System.Collections.Generic;

namespace AppLogic.Inscripciones.Dtos
{
    public class DtoConfirmarPreInscripcionRequest
    {
        public bool AceptoReglamento { get; set; }

        /// <summary>
        /// Ofertas seleccionadas a confirmar. Para productos de nivel 1 y 2 debe traer exactamente una;
        /// para nivel 3 y 4 puede traer varias.
        /// </summary>
        public List<long> IdsOfertasSeleccionadas { get; set; } = new();
    }
}
