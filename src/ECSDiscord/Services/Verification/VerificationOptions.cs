using System.ComponentModel.DataAnnotations;

namespace ECSDiscord.Services.Verification;

public class VerificationOptions
{
    public static string Name => "Verification";

    [Required(AllowEmptyStrings = false)]
    public string EmailPattern { get; init; }
    
    [Required]
    public int UsernamePatternGroup { get; init; }
    
    [Required(AllowEmptyStrings = false)]
    public string MailSubjectTemplate { get; init; }
    
    [Required(AllowEmptyStrings = false)]
    public string MailBodyTemplate { get; init; }
    
    [Required]
    public bool MailBodyIsHtml { get; init; }
    
    [Required]
    public ulong VerifiedRoleId { get; init; }
    
    [Required]
    public ulong MutedRoleId { get; init; }
    
    [Required(AllowEmptyStrings = false)]
    public string PublicKeyPath { get; init; }
}