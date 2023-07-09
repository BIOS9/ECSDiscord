using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using ECSDiscord.Services.Modals;
using ECSDiscord.Services.Modals.Modals;

namespace ECSDiscord.Services.SlashCommands.Commands;

public class ResetCourseCommand : ISlashCommand
{
    private readonly ModalsHandler _modalsHandler;

    public ResetCourseCommand(ModalsHandler modalsHandler)
    {
        _modalsHandler = modalsHandler ?? throw new ArgumentNullException(nameof(modalsHandler));
    }

    public string Name => "resetcourse";

    public SlashCommandProperties Build()
    {
        return new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Wipes content of course channels while retaining users.")
            .WithDefaultMemberPermissions(GuildPermission.Administrator)
            .WithDMPermission(false)
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("current")
                .WithDescription("Reset the current course channel.")
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("select")
                .WithDescription("Opens selection menu to chose channels to reset.")
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("regex")
                .WithDescription("Reset all course channels matching a regex pattern (DANGER!!).")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("pattern")
                    .WithDescription("The regex pattern of the courses you want to reset.")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.String)))
            .Build();
    }

    public async Task ExecuteAsync(ISlashCommandInteraction command)
    {
        if (command.IsDMInteraction || command.ChannelId == null)
        {
            await command.RespondAsync("This command must be run in a guild channel.", ephemeral: true);
            return;
        } 
        
        var actionName = command.Data.Options.First().Name;

        switch (actionName)
        {
            case "current":
                await ExecuteCurrentAsync(command);
                break;
            case "select":
                await ExecuteSelectAsync(command);
                break;
            case "regex":
                await ExecuteRegexAsync(command);
                break;
            default:
                await command.RespondAsync("Unknown sub-command.", ephemeral: true);
                break;
        }
    }

    private async Task ExecuteCurrentAsync(ISlashCommandInteraction command)
    {
        var builder = new ComponentBuilder()
            .WithRows(new[]
            {
                new ActionRowBuilder()
                    .WithButton("Confirm", "confirm", ButtonStyle.Danger)
                    .WithButton("Cancel", "cancel", ButtonStyle.Secondary)
            });
        
        await command.RespondAsync("##  :warning: Are you sure you want to reset the current channel?  :warning:\n" +
                                   "This means all of the following will be **__deleted__**:\n" +
                                   "* messages\n" +
                                   "* attachments\n" +
                                   "* images\n" +
                                   "* threads\n" +
                                   "* pins", components: builder.Build(), ephemeral: true);
    }
    
    private async Task ExecuteSelectAsync(ISlashCommandInteraction command)
    {
        var builder = new ComponentBuilder()
            .WithRows(new[]
            {
                new ActionRowBuilder()
                    .WithSelectMenu(
                        new SelectMenuBuilder()
                            .WithCustomId("channels")
                            .WithType(ComponentType.ChannelSelect)
                            .WithChannelTypes(ChannelType.Text)
                            .WithMaxValues(int.MaxValue)),
                        
                new ActionRowBuilder()
                    .WithButton("Confirm", "confirm", ButtonStyle.Danger)
                    .WithButton("Cancel", "cancel", ButtonStyle.Secondary)
            });
    }

    private async Task ExecuteRegexAsync(ISlashCommandInteraction command)
    {
        
    }
}