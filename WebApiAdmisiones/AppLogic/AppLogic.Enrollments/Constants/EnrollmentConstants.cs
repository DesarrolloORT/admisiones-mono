namespace AppLogic.Enrollments.Constants;

public static class EnrollmentConstants
{
    public static class PaymentType
    {
        public const string Banred = "BANRED";
        public const string Sistarbanc = "SISTARBANC";
        public const string Geopay = "GEOPAY";
        public const string Abitab = "ABITAB";
        public const string Paganza = "PAGANZA";
        public const string CuentaPersonal = "CUENTA_PERSONAL";
    }

    /// <summary>Valores del campo <c>Resultado</c> de <c>StartPaymentResponse</c> que interpreta el frontend.</summary>
    public static class PaymentResult
    {
        public const string PagoConfirmado = "PAGO_CONFIRMADO";
        public const string MetodoGuardado = "METODO_GUARDADO";
        public const string UrlGenerada = "URL_GENERADA";
    }

    public static class EnrollmentType
    {
        public const string Online = "ONLINE";
    }

    public static class ProductInterest
    {
        public const string ObservacionesWeb = "ALTA DESDE ADMISIONES WEB";
        public const int FormaContactoWeb = 7;
        public const int LugarInteresWeb = 8;
    }

    public static class CorporateInbox
    {
        public const long IdProceso = 89;
        public const long IdEstadoProcesoInicio = 59643;
        public const long IdEstadoProcesoSolicitud = 59644;
        public const long IdGrupoResponsable = 48;
        public const string UsuarioSistema = "ADMISIONES";
    }
}
