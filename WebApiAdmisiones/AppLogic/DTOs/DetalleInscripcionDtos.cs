namespace AppLogic.DTOs
{
    /// <summary>
    /// Detalle de una inscripción de "Mis carreras". El contenido depende del estado:
    /// "En proceso" trae la oferta seleccionada; "A la espera" no trae detalle;
    /// "Pago pendiente" y "Confirmada" se completan en una tanda posterior (servicios LogicaORT).
    /// </summary>
    public class DetalleInscripcionResponse
    {
        public string Estado { get; set; } = string.Empty;

        /// <summary>Oferta seleccionada. Solo se completa cuando el estado es "En proceso".</summary>
        public ResumenInscripcionDto? Detalle { get; set; }
    }
}
