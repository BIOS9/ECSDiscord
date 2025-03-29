using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using ECSDiscord.Services.Translations;
using Serilog;

namespace ECSDiscord.Services.SlashCommands.Commands;

public class MinecraftCommand : ISlashCommand
{
    private readonly MinecraftService _minecraftService;

    public MinecraftCommand(MinecraftService minecraftService)
    {
        _minecraftService = minecraftService ?? throw new ArgumentNullException(nameof(minecraftService));
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
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("username", ApplicationCommandOptionType.String, "Your Minecraft username.", true))
            .Build();
    }

    public async Task ExecuteAsync(ISlashCommandInteraction command)
    {
        var username = (string)command.Data.Options.First().Options.First().Value;
        var uuid = await _minecraftService.QueryMinecraftUuidAsync(username);
        if (uuid == null)
        {
            await command.RespondAsync(":warning:  A Minecraft account with that username count not be found!", ephemeral: true);
            return;
        }

        var verifyResult = await _minecraftService.VerifyMinecraftAccountAsync(uuid.Value, command.User, false);
        switch (verifyResult)
        {
            case MinecraftService.VerificationResult.Success:
                await command.RespondAsync(":white_check_mark:  Minecraft account verified successfully!", ephemeral: true);
                break;
            case MinecraftService.VerificationResult.AlreadyVerified:
                await command.RespondAsync(":information_source:  That Minecraft account has already been verified.", ephemeral: true);
                break;
            case MinecraftService.VerificationResult.VerificationLimitReached:
                await command.RespondAsync(":no_entry_sign:  You cannot verify any more Minecraft accounts. Please ask the admins if you want to switch accounts.", ephemeral: true);
                break;
            case MinecraftService.VerificationResult.DiscordNotVerified:
                await command.RespondAsync(":warning:  Your Discord account must be linked to your Uni email before you can use this command.\nPlease use `/verify yourusername@myvuw.ac.nz` to verify your account.", ephemeral: true);
                break;
            default:
                Log.Warning("Unexpected minecraft verification result: {Result}", verifyResult);
                await command.RespondAsync(":fire:  Something went wrong! Please ask one of the admins to check the logs.", ephemeral: true);
                break;
        }
    }
}