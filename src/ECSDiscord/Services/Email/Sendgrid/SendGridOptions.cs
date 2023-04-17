using System.ComponentModel.DataAnnotations;

namespace ECSDiscord.Services.Email.Sendgrid;

public class SendGridOptions
{
    public static string Name => "SendGrid";

    [Required(AllowEmptyStrings = false)]
    public string ApiKey { get; init; }
    
    [Required(AllowEmptyStrings = false)]
    public string FromAddress { get; init; }
    
    [Required(AllowEmptyStrings = false)]
    public string FromName { get; init; }
    
    

}