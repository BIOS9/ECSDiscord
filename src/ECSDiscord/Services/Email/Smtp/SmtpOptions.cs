using System.ComponentModel.DataAnnotations;

namespace ECSDiscord.Services.Email.Smtp;

public class SmtpOptions
{
    public static string Name => "Smtp";

    [Required(AllowEmptyStrings = false)] public string Server { get; init; }
    [Required(AllowEmptyStrings = false)] public int Port { get; init; }
    [Required(AllowEmptyStrings = false)] public bool Ssl { get; init; }
    [Required(AllowEmptyStrings = false)] public string Username { get; init; }
    [Required(AllowEmptyStrings = false)] public string Password { get; init; }
    [Required(AllowEmptyStrings = false)] public string FromAddress { get; init; }
    [Required(AllowEmptyStrings = false)] public string FromName { get; init; }
}