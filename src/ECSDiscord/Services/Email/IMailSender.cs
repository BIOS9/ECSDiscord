using System.Threading.Tasks;

namespace ECSDiscord.Services.Email;

public interface IMailSender
{
    public Task<bool> SendMailAsync(string recipientAddress, string subject, string body, bool bodyIsHtml = false);
}