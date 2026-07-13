namespace AppLogic.ApiClients.Dtos
{
    /// <summary>
    /// DTO para par clave-valor usado en carritos de pago.
    /// Usado en: POST /Pagos/Carritos/Pagar y POST /Carritos/UrlCrearFactura
    /// </summary>
    public class ClaveValorCarrito
    {
        public string Clave { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
        public string ClaveCarrito { get; set; } = string.Empty;
        public string CantidadCuotasAPagar { get; set; } = string.Empty;
        public string Banco { get; set; } = string.Empty;
    }
}
