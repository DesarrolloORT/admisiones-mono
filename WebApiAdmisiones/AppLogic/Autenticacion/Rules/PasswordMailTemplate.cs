using System.Text;
using System.Text.Encodings.Web;
using BusinessLogic.Entities;
using MailORT;

namespace AppLogic.Autenticacion.Rules;

public static class PasswordMailTemplate
{
    private const string ParrafoSeguridad = "Tu contrase&ntilde;a es privada y su uso es estrictamente personal. Por tu seguridad, no la compartas con nadie. ORT nunca te solicitar&aacute; actualizar tu usuario, contrase&ntilde;a o datos de medios de pago electr&oacute;nicos por e-mail, tel&eacute;fono, SMS, WhatsApp ni redes sociales. M&aacute;s informaci&oacute;n en: <a href=\"https://www.ort.edu.uy/ciberseguridad\" target=\"_blank\" rel=\"noopener noreferrer\">www.ort.edu.uy/ciberseguridad</a>.";

    public static string ConstruirMailActivacion(Persona persona, string link)
    {
        var nombre = HtmlEncoder.Default.Encode(persona.PrimerNombre?.Trim() ?? "usuario");
        var safeLink = HtmlEncoder.Default.Encode(link);

        return ConstruirBodyMailInstitucional(
            "Solicitud de numero de usuario y contrase&ntilde;a",
            $"Estimado/a {nombre}:",
            new[]
            {
                $"Tu registro se ha realizado con exito. Para crear tu contrase&ntilde;a e ingresar al sitio, hac&eacute; clic en el siguiente enlace:<br><br>{ConstruirLinkHtml(safeLink, "Crear contrase&ntilde;a")}",
                "Te recordamos que en este sitio podr&aacute;s comenzar el proceso de inscripci&oacute;n a una carrera universitaria o corta, as&iacute; como tambi&eacute;n acceder a los fondos de becas disponibles.",
                "Por seguridad, el link vence en el plazo indicado por el sistema y puede usarse una sola vez.",
                ParrafoSeguridad,
                "Atentamente,<br>Departamento de Admisiones"
            });
    }

    public static string ConstruirMailRecuperacion(Persona persona, string link)
    {
        var nombre = HtmlEncoder.Default.Encode(persona.PrimerNombre?.Trim() ?? "usuario");
        var safeLink = HtmlEncoder.Default.Encode(link);

        return ConstruirBodyMailInstitucional(
            "Recupero de contrase&ntilde;a",
            $"Estimado/a {nombre}:",
            new[]
            {
                $"Recibimos una solicitud para recuperar tu contrase&ntilde;a de Admisiones. Para crear una nueva contrase&ntilde;a e ingresar al sitio, hac&eacute; clic en el siguiente enlace:<br><br>{ConstruirLinkHtml(safeLink, "Recuperar contrase&ntilde;a")}",
                "Por seguridad, el link vence en el plazo indicado por el sistema y puede usarse una sola vez.",
                "Si no solicitaste este cambio, pod&eacute;s ignorar este mensaje.",
                ParrafoSeguridad,
                "Atentamente,<br>Departamento de Admisiones"
            });
    }

    /// <summary>
    /// Versión del mail de activación para personas nuevas (no tienen entidad Persona en DB aún).
    /// </summary>
    public static string ConstruirMailActivacionSinPersona(string link)
    {
        var safeLink = HtmlEncoder.Default.Encode(link);

        return ConstruirBodyMailInstitucional(
            "Solicitud de numero de usuario y contrase&ntilde;a",
            "Estimado/a:",
            new[]
            {
                $"Tu registro se ha realizado con exito. Para crear tu contrase&ntilde;a e ingresar al sitio, hac&eacute; clic en el siguiente enlace:<br><br>{ConstruirLinkHtml(safeLink, "Crear contrase&ntilde;a")}",
                "Te recordamos que en este sitio podr&aacute;s comenzar el proceso de inscripci&oacute;n a una carrera universitaria o corta, as&iacute; como tambi&eacute;n acceder a los fondos de becas disponibles.",
                "Por seguridad, el link vence en el plazo indicado por el sistema y puede usarse una sola vez.",
                ParrafoSeguridad,
                "Atentamente,<br>Departamento de Admisiones"
            });
    }

    private static string ConstruirBodyMailInstitucional(
        string titulo,
        string destinatario,
        IEnumerable<string> parrafos)
    {
        var sb = new StringBuilder();
        sb.Append(EnvioMail.CabezalHTML());
        sb.Append(EnvioMail.Cabezal());
        sb.Append(EnvioMail.CuerpoConTitulo(titulo, DateTime.Now, destinatario));

        foreach (var parrafo in parrafos)
        {
            sb.Append(EnvioMail.CuerpoParrafos(parrafo));
        }

        sb.Append(EnvioMail.FinHtml);
        return sb.ToString();
    }

    private static string ConstruirLinkHtml(string safeLink, string texto)
    {
        return $"<a href=\"{safeLink}\" target=\"_blank\" rel=\"noopener noreferrer\">{texto}</a>";
    }
}
