namespace AppLogic.Common.Email;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body);
}
