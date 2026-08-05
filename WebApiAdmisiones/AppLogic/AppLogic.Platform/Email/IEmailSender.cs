namespace AppLogic.Platform.Email;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body);
}
