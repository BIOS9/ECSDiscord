using System.ComponentModel.DataAnnotations;

namespace ECSDiscord.Services.Bot;

public class DiscordBotOptions
{
    public static string Name => "DiscordBot";

    [Required(AllowEmptyStrings = false)]
    public string Token { get; init; }
    
    [Required]
    [Range(1, ulong.MaxValue)]
    public ulong GuildId { get; init; }
    
    [StringLength(128)]
    public string StatusText { get; init; } = string.Empty;
    
}