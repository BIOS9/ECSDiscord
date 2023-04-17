using System.ComponentModel.DataAnnotations;

namespace ECSDiscord.Services.Email.Sendgrid;

public class SendGridOptions
{
    public static string Name => "SendGrid";

    [Required(AllowEmptyStrings = false)]
    public string Token { get; init; }
    
    [Required]
    [Range(1, ulong.MaxValue)]
    public ulong GuildId { get; init; }
    
    [StringLength(128)]
    public string StatusText { get; init; } = string.Empty;
    
}