using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;

public class HotmailEmailClient
{
    private readonly ILogger _logger;

    public string From { get; set; }
    public string To { get; set; }
    public string SmtpServer { get; set; }
    public int SmtpPort { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }

    public HotmailEmailClient(
        string from,
        string to,
        string smtpServer,
        int smtpPort,
        string username,
        string password,
        ILogger logger)
    {
        From = from;
        To = to;
        SmtpServer = smtpServer;
        SmtpPort = smtpPort;
        Username = username;
        Password = password;
        _logger = logger;
    }

    public void SendEmail(string subject, string body)
    {
        using var mailMessage = new MailMessage(From, To)
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = true,
            Priority = MailPriority.High
        };

        var mailSender = new SmtpClient(SmtpServer, SmtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(Username, Password)
        };

        try
        {
            mailSender.Send(mailMessage);
        }
        catch (System.Exception ex)
        {
            _logger.LogError($"Send mail with error: {ex.Message}");
        }
    }
}