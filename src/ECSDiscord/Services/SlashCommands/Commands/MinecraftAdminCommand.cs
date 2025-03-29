using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using ECSDiscord.Services.Translations;
using Serilog;

namespace ECSDiscord.Services.SlashCommands.Commands;

public class MinecraftAdminCommand : ISlashCommand
{
    private readonly MinecraftService _minecraftService;

    public MinecraftAdminCommand(MinecraftService minecraftService)
    {
        _minecraftService = minecraftService ?? throw new ArgumentNullException(nameof(minecraftService));
    }

    public string Name => "minecraftadmin";

    public SlashCommandProperties Build()
    {
        return new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Admin commands related to the Unofficial ECS Minecraft Server.")
            .WithDefaultMemberPermissions(GuildPermission.Administrator)
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("verifications_list")
                .WithDescription("Lists all verified Minecraft accounts.")
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("verifications_delete")
                .WithDescription("Deletes a verified Minecraft account.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("minecraftuuid", ApplicationCommandOptionType.String, "Minecraft UUID to delete.", true))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("verifications_create")
                .WithDescription("Verifies a Minecraft account.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("minecraftuuid", ApplicationCommandOptionType.String, "Minecraft UUID to verify.", true)
                .AddOption("user", ApplicationCommandOptionType.User, "Discord user to link to the account.", true)
                .AddOption("isexternal", ApplicationCommandOptionType.Boolean, "Whether the Minecraft account belongs to a user outside of this Discord server.", true))
            .Build();
    }

    public async Task ExecuteAsync(ISlashCommandInteraction command)
    {
        string subCommand = command.Data.Options.First().Name;

        switch (subCommand)
        {
            case "verifications_list":
                await HandleListVerificationsAsync(command);
                break;
            case "verifications_delete":
                await HandleDeleteVerificationAsync(command);
                break;
            case "verifications_create":
                await HandleCreateVerificationAsync(command);
                break;
            default:
                await command.RespondAsync("Unknown subcommand.", ephemeral: true);
                break;
        }
    }

    private async Task HandleListVerificationsAsync(ISlashCommandInteraction command)
    {
        var accounts = await _minecraftService.GetAllMinecraftAccountsAsync();
        
        if (!accounts.Any())
        {
            await command.RespondAsync("No verified Minecraft accounts found.", ephemeral: true);
            return;
        }
        
        var response = new StringBuilder("**Verified Minecraft Accounts:**\n");
        foreach (var account in accounts)
        {
            response.AppendLine($"- **UUID:** `{account.MinecraftUuid}` | **Discord:** {account.DiscordUser.Username} | **External:** {account.IsExternal}");
        }
        
        await command.RespondAsync(response.ToString(), ephemeral: true);
    }

    private async Task HandleDeleteVerificationAsync(ISlashCommandInteraction command)
    {
        string minecraftUuid = command.Data.Options.First().Options.First().Value.ToString();

        if (!Guid.TryParse(minecraftUuid, out Guid guid))
        {
            await command.RespondAsync($":warning:  Invalid Minecraft UUID {minecraftUuid}!", ephemeral: true);
            return;
        }
        
        if (await _minecraftService.DeleteMinecraftAccountAsync(guid))
        {
            await command.RespondAsync(":white_check_mark:  Minecraft account verification deleted successfully!", ephemeral: true);
        }
        else
        {
            await command.RespondAsync($":warning:  Could not find a Minecraft account verification with the UUID {minecraftUuid}!", ephemeral: true);
        }
    }

    private async Task HandleCreateVerificationAsync(ISlashCommandInteraction command)
    {
        var options = command.Data.Options.First().Options;
        string minecraftUuid = options.First(o => o.Name == "minecraftuuid").Value.ToString();
        IUser user = (IUser)options.First(o => o.Name == "user").Value;
        bool isExternal = (bool)options.First(o => o.Name == "isexternal").Value;
        
        if (!Guid.TryParse(minecraftUuid, out Guid guid))
        {
            await command.RespondAsync($":warning:  Invalid Minecraft UUID {minecraftUuid}!", ephemeral: true);
            return;
        }
        
        var verifyResult = await _minecraftService.VerifyMinecraftAccountAsync(guid, user, isExternal);
        switch (verifyResult)
        {
            case MinecraftService.VerificationResult.Success:
                await command.RespondAsync(":white_check_mark:  Minecraft account verified successfully!", ephemeral: true);
                break;
            case MinecraftService.VerificationResult.AlreadyVerified:
                await command.RespondAsync(":information_source:  That Minecraft account has already been verified.", ephemeral: true);
                break;
            case MinecraftService.VerificationResult.VerificationLimitReached:
                await command.RespondAsync(":no_entry_sign:  A Discord user can only be linked to a single **internal** Minecraft account.\nEither delete the existing verification, or if the Minecraft account is owned by someone outside of this Discord server you can link the account as **external**.", ephemeral: true);
                break;
            case MinecraftService.VerificationResult.DiscordNotVerified:
                await command.RespondAsync(":fire:  Somehow you're not verified but have managed to run this command. This is a bug.", ephemeral: true);
                break;
            default:
                Log.Warning("Unexpected minecraft verification result: {Result}", verifyResult);
                await command.RespondAsync(":fire:  Something went wrong! Please ask someone who has access to the bot logs for help.", ephemeral: true);
                break;
        }
    }
}