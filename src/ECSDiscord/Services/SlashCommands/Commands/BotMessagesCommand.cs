using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using ECSDiscord.Services.Modals;
using ECSDiscord.Services.Modals.Modals;
using ECSDiscord.Services.Translations;
using ECSDiscord.Util;
using Humanizer;
using Microsoft.VisualBasic;

namespace ECSDiscord.Services.SlashCommands.Commands;

public class BotMessagesCommand : ISlashCommand
{
    private readonly ServerMessageService _serverMessageService;
    private readonly ModalsHandler _modalsHandler;
    
    public BotMessagesCommand(
        ServerMessageService serverMessageService,
        ModalsHandler modalsHandler)
    {
        _serverMessageService = serverMessageService ?? throw new ArgumentNullException(nameof(serverMessageService));
        _modalsHandler = modalsHandler ?? throw new ArgumentNullException(nameof(modalsHandler));
    }

    public string Name => "botmessages";

    public SlashCommandProperties Build()
    {
        return new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Sends and edits messages sent using this bot.")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("create")
                .WithDescription("Create a new bot message in the current channel.")
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("delete")
                .WithDescription("Deletes an existing bot message in the current channel.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("message_id")
                    .WithDescription("The ID of the bot message you want to delete.")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.String)))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("edit")
                .WithDescription("Edits an existing bot message in the current channel.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("message_id")
                    .WithDescription("The ID of the bot message you want to edit.")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.String)))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("list")
                .WithDescription("Lists all bot messages in the current channel.")
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
            case "create":
                await command.RespondWithModalAsync(
                    await _modalsHandler.RegisterModalAsync(new BotMessageCreateModal(_serverMessageService)));
                break;
            case "delete":
                await DeleteMessageAsync(command);
                break;
            case "edit":
                await EditMessageAsync(command);
                break;
            case "list":
                await ListMessagesAsync(command);
                break;
            default:
                await command.RespondAsync("Unknown sub-command.", ephemeral: true);
                break;
        }
    }

    private async Task DeleteMessageAsync(ISlashCommandInteraction command)
    {
        await command.DeferAsync(ephemeral: true);
        string messageIdStr = (string)command.Data.Options.First().Options.First().Value;
        if (!ulong.TryParse(messageIdStr, out ulong messageId))
        {
            await command.FollowupAsync("Invalid message ID.", ephemeral: true);
            return;
        }
        
        if (!await _serverMessageService.IsMessagePresentAsync(messageId))
        {
            await command.FollowupAsync("That message ID does not exist.", ephemeral: true);
            return;
        }

        await _serverMessageService.DeleteMessageAsync(messageId);
        await command.FollowupAsync("Message deleted.", ephemeral: true);
    }
    
    private async Task EditMessageAsync(ISlashCommandInteraction command)
    {
        string messageIdStr = (string)command.Data.Options.First().Options.First().Value;
        if (!ulong.TryParse(messageIdStr, out ulong messageId))
        {
            await command.RespondAsync("Invalid message ID.", ephemeral: true);
            return;
        }
        
        if (!await _serverMessageService.IsMessagePresentAsync(messageId))
        {
            await command.RespondAsync("That message ID does not exist.", ephemeral: true);
            return;
        }
        await command.RespondWithModalAsync(
            await _modalsHandler.RegisterModalAsync(new BotMessageEditModal(messageId, _serverMessageService)));
    }
    
    private async Task ListMessagesAsync(ISlashCommandInteraction command)
    {
        await command.DeferAsync(ephemeral: true);
        var messages = await _serverMessageService.GetAllMessagesAsync();
        List<EmbedFieldBuilder> fields = new();
        foreach (var message in messages)
        {
            if (command.ChannelId == null || command.ChannelId != command.ChannelId.Value)
            {
                continue;
            }
            
            fields.Add(new EmbedFieldBuilder()
                .WithName($"{message.Name}")
                .WithValue($"ID: **{message.ID}**\n" +
                           $"Created by {message.Creator.Mention} **{message.CreatedAt.Humanize()}**\n" +
                           $"Last edited by {message.Editor.Mention} **{message.EditedAt.Humanize()}**\n" +
                           $"```{message.Content.Truncate(30, true)}```"));
        }

        if (fields.Any())
        {
            await command.FollowupAsync(embed: new EmbedBuilder()
                    .WithTitle($"Bot messages in this channel:")
                    .WithFields(fields)
                    .WithColor(new Color(15, 84, 53))
                    .Build(),
                ephemeral: true);
        }
        else
        {
            await command.FollowupAsync("There are no bot messages in this channel.", ephemeral: true);
        }
    }
}