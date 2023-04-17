using System.ComponentModel.DataAnnotations;

namespace ECSDiscord.Services.Bot;

public class DiscordBotOptions
{
    public static string Name => "DiscordBot";

    [Required]
    public string Token { get; init; }
    
    [Required]
    public ulong GuildId { get; init; }
    
    [StringLength(128)]
    public string StatusText { get; init; } = string.Empty;
    
}