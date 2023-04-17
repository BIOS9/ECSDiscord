using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace ECSDiscord.Services.Email.Sendgrid;

public class SendGridMailSender : IMailSender
{
    private readonly ILogger<SendGridMailSender> _logger;
    private readonly SendGridOptions _options;

    public SendGridMailSender(IOptions<SendGridOptions> options, ILogger<SendGridMailSender> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> SendMailAsync(string recipientAddress, string subject, string body, bool bodyIsHtml = false)
    {
        _logger.LogDebug("Sending email to {Recipient}", recipientAddress);
        var client = new SendGridClient(_options.ApiKey);
        var msg = new SendGridMessage
        {
            From = new EmailAddress(_options.FromAddress, _options.FromName),
            Subject = subject
        };

        if (bodyIsHtml)
            msg.HtmlContent = body;
        else
            msg.PlainTextContent = body;

        msg.AddTo(new EmailAddress(recipientAddress));
        var response = await client.SendEmailAsync(msg);

        return response.IsSuccessStatusCode;
    }
}