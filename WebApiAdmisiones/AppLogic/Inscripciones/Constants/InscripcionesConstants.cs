namespace AppLogic.Inscripciones.Constants
{
    public static class InscripcionesConstants
    {
        /// <summary>
        /// Valores de ESTADO_INSCRIPCION que devuelven las vistas VD_INSCRIPCIONES_FRESCO_1Y2 / _3Y4.
        /// Verificar la grafía exacta contra la definición de la vista Oracle (máx. 14 chars).
        /// </summary>
        public static class EstadoInscripcion
        {
            public const string EnProceso = "En proceso";
            public const string PagoPendiente = "Pago pendiente";
            public const string ALaEspera = "A la espera";
            public const string Confirmada = "Confirmada";
            public const string DadaDeBaja = "Dada de baja";
        }

        public static class TipoPago
        {
            public const string Banred = "BANRED";
            public const string Sistarbanc = "SISTARBANC";
            public const string Geopay = "GEOPAY";
            public const string Abitab = "ABITAB";
            public const string Paganza = "PAGANZA";
        }

        public static class InteresProducto
        {
            public const string ObservacionesWeb = "ALTA DESDE ADMISIONES WEB";
            public const string FormatoHora = "HH:mm:ss";
            public const string TipoPersonaSgi = "SGI";
            public const string CodigoVigenciaActiva = "SI";
            public const int EstadoSolicitudPendiente = 1;
            public const int CodigoFuenteDatosAdmisiones = 170;
            public const int FormaContactoWeb = 7;
            public const int TipoAccionRegistroSitioAdmisiones = 109;
            public const int LugarInteresWeb = 8;
        }

        public static class BandejaCorporativa
        {
            public const long IdProceso = 89;
            public const long IdEstadoProcesoInicio = 57156;
            public const long IdEstadoProcesoSolicitud = 57157;
            public const long IdGrupoResponsable = 48;
            public const string UsuarioSistema = "ADMISIONES";
        }
    }
}
