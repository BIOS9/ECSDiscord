using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using ECSDiscord.Services.Bot;
using ECSDiscord.Util;

namespace ECSDiscord.Services.SlashCommands.Commands;

public class ResetCourseCommand : ISlashCommand
{
    private readonly DiscordBot _discordBot;
    private readonly CourseService _courseService;
    private readonly EnrollmentsService _enrollmentsService;
    
    public ResetCourseCommand(DiscordBot discordBot, CourseService courseService, EnrollmentsService enrollmentsService)
    {
        _discordBot = discordBot ?? throw new ArgumentNullException(nameof(discordBot));
        _courseService = courseService ?? throw new ArgumentNullException(nameof(courseService));
        _enrollmentsService = enrollmentsService ?? throw new ArgumentNullException(nameof(enrollmentsService));
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
            default:
                await command.RespondAsync("Unknown sub-command.", ephemeral: true);
                break;
        }
    }

    private async Task ExecuteCurrentAsync(ISlashCommandInteraction command)
    {
        var channel = await _discordBot.DiscordClient
            .GetChannelAsync(command.ChannelId ?? throw new ArgumentNullException("ChannelId"));
        if (!await _courseService.CourseExists(channel.Name))
        {
            await command.RespondAsync(":no_entry_sign:  Current channel is not a course channel.", ephemeral: true);
            return;
        }
        
        string confirmId = "coursereset-confirm" + StringExtensions.RandomString(50);
        string cancelId = "coursereset-cancel" + StringExtensions.RandomString(50);

        var components = new ComponentBuilder()
            .WithRows(new[]
            {
                new ActionRowBuilder()
                    .WithButton("Confirm", confirmId, ButtonStyle.Danger)
                    .WithButton("Cancel", cancelId, ButtonStyle.Secondary)
            });

        async Task ButtonEventHandler(SocketMessageComponent component)
        {
            if (component.Data.CustomId == confirmId)
            {
                _discordBot.DiscordClient.ButtonExecuted -= ButtonEventHandler;
                await component.UpdateAsync(properties =>
                {
                    properties.Content = "## :white_check_mark:  Channel reset in progress.";
                    properties.Components = null;
                });
                await ResetCourseChannel((SocketTextChannel)await component.GetChannelAsync());
            }
            else if (component.Data.CustomId == cancelId)
            {
                _discordBot.DiscordClient.ButtonExecuted -= ButtonEventHandler;
                await component.UpdateAsync(properties =>
                {
                    properties.Content = "## :no_entry_sign:  Channel reset cancelled.";
                    properties.Components = null;
                });
            }
        }

        _discordBot.DiscordClient.ButtonExecuted += ButtonEventHandler;
        
        await command.RespondAsync("## :warning:  Are you sure you want to reset the current channel?  :warning:\n" +
                                   "This means all of the following will be **__deleted__**:\n" +
                                   "* messages\n" +
                                   "* attachments\n" +
                                   "* images\n" +
                                   "* threads\n" +
                                   "* pins", components: components.Build(), ephemeral: true);
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
        await command.RespondAsync("Please choose which channels you want to reset.", components: builder.Build(), ephemeral: true);
    }

    private async Task ResetCourseChannel(SocketTextChannel channel)
    {
        var members = await _enrollmentsService.GetCourseMembers(channel.Name);
        await _courseService.RemoveCourseAsync(channel.Name);
        await channel.DeleteAsync();
        await _courseService.CreateCourseAsync(channel.Name);
        await _enrollmentsService.AddCourseMembers(channel.Name, members.Select(x => x.Id).ToList());
    }
} 