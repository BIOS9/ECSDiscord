using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;

namespace ECSDiscord.Services.SlashCommands.Commands;

public class VerifyCommand : ISlashCommand
{
    private readonly VerificationService _verificationService;

    public VerifyCommand(VerificationService verificationService)
    {
        _verificationService = verificationService ?? throw new ArgumentNullException(nameof(verificationService));
    }

    public string Name => "verify";

    public SlashCommandProperties Build()
    {
        return new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Associated your VUW username with your ECS discord account.")
            .AddOption(
                "email",
                ApplicationCommandOptionType.String,
                "Your VUW student email address. E.g. username@myvuw.ac.nz",
                true)
            .Build();
    }

    public async Task ExecuteAsync(ISlashCommandInteraction command)
    {
        var email = (string)command.Data.Options.First().Value;
        var result = await _verificationService.StartVerificationAsync(email, command.User);
        switch (result)
        {
            case VerificationService.EmailResult.InvalidEmail:
                await command.RespondAsync(
                    ":warning:  Invalid email address.\nPlease use a VUW student email address. e.g. `username@myvuw.ac.nz`",
                    ephemeral: true);
                break;
            case VerificationService.EmailResult.Success:
                await command.RespondAsync(
                    ":white_check_mark:  Verification email sent!\nPlease check your email for further instructions.",
                    ephemeral: true);
                break;
            case VerificationService.EmailResult.Failure:
            default:
                await command.RespondAsync(":fire:  A server error occured. Please ask an admin to check the logs.",
                    ephemeral: true);
                break;
        }
    }
}