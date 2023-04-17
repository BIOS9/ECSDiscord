using System.ComponentModel.DataAnnotations;

namespace ECSDiscord.Services.PrefixCommands;

public class PrefixCommandsOptions
{
    public static string Name => "PrefixCommands";

    [Required(AllowEmptyStrings = false)]
    public string Prefix { get; init; }
}