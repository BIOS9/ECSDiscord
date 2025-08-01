using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace ECSDiscord.Services.Email.Smtp;

public class SmtpMailSender : IMailSender
{
    private readonly ILogger<SmtpMailSender> _logger;
    private readonly SmtpOptions _options;

    public SmtpMailSender(IOptions<SmtpOptions> options, ILogger<SmtpMailSender> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> SendMailAsync(string recipientAddress, string subject, string body, bool bodyIsHtml = false)
    {
        _logger.LogDebug(recipientAddress);
        _logger.LogDebug(_options.FromAddress);
        _logger.LogDebug(_options.FromName);
        _logger.LogDebug(_options.Username);
        _logger.LogDebug(_options.Password);
        _logger.LogDebug(_options.Server);
        _logger.LogDebug(_options.Port.ToString());
        _logger.LogDebug(_options.Ssl.ToString());
        _logger.LogDebug(subject);
        _logger.LogDebug(body);

        _logger.LogDebug("Sending email to {Recipient}", recipientAddress);
        try
        {
            var mail = new MailMessage();
            var client = new SmtpClient(_options.Server, _options.Port) //Port 8025, 587 and 25 can also be used.
            {
                Credentials = new NetworkCredential(_options.Username, _options.Password),
                EnableSsl = _options.Ssl
            };
            mail.From = new MailAddress(_options.FromAddress, _options.FromName);
            mail.To.Add(recipientAddress);
            mail.Subject = subject;
            mail.IsBodyHtml = bodyIsHtml;
            mail.Body = body;

            await client.SendMailAsync(mail);
            _logger.LogInformation("Email sent successfully to {Recipient}", recipientAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP error for recipient {}", recipientAddress);
            return false;
        }

        return true;
    }
}