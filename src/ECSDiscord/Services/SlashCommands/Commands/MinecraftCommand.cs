using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;

namespace ECSDiscord.Services.SlashCommands.Commands;

public class MinecraftCommand : ISlashCommand
{
    private readonly VerificationService _verificationService;

    public MinecraftCommand(VerificationService verificationService)
    {
        _verificationService = verificationService ?? throw new ArgumentNullException(nameof(verificationService));
    }

    public string Name => "minecraft";

    public SlashCommandProperties Build()
    {
        return new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Commands related to the Unofficial ECS Minecraft Server.")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("verify")
                .WithDescription("Associate your Minecraft account with your ECS Discord account.")
                .AddOption("username", ApplicationCommandOptionType.String, "Your Minecraft username.", true))
            .Build();
    }

    public async Task ExecuteAsync(ISlashCommandInteraction command)
    {
        var username = (string)command.Data.Options.First().Value;
        await command.RespondAsync("You entered " + username);
    }
}