namespace AppLogic.IServices;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body);
}
