using MailORT;
using Microsoft.Extensions.Configuration;

namespace AppLogic.Common.Email;

/// <summary>
/// Implementación de IEmailSender que delega en EnvioMail (SOAP Office365).
/// Lee el remitente desde la configuración Mail:From.
/// </summary>
public class EnvioMailEmailSender : IEmailSender
{
    private const string SistemaMail = "ADMISIONES";

    private readonly EnvioMail _envioMail;
    private readonly IConfiguration _configuration;

    public EnvioMailEmailSender(EnvioMail envioMail, IConfiguration configuration)
    {
        _envioMail = envioMail;
        _configuration = configuration;
    }

    public Task SendAsync(string to, string subject, string body)
    {
        var from = _configuration["Mail:From"] ?? "admisiones@ort.edu.uy";
        return _envioMail.EnviarMail(
            from,
            [to.Trim()],
            subject,
            body,
            sistema: SistemaMail);
    }
}
