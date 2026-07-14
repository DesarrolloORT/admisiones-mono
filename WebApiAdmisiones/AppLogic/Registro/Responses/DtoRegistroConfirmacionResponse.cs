using System.Diagnostics.CodeAnalysis;

namespace AppLogic.Registro.Responses
{
    /// <summary>
    /// Confirma que el registro se completó. <see cref="MailEnviado"/> distingue éxito parcial
    /// (registro OK, mail de activación no enviado) de éxito completo (SRV-05).
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DtoRegistroConfirmacionResponse
    {
        public bool MailEnviado { get; set; } = true;
    }
}
