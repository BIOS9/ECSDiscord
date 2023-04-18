using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace ECSDiscord.Services.Modals.Modals;

public class BotMessageCreateModal : IModal
{
    public string Name => "bot_message_create";
    public string CustomId { get; set; }

    private readonly ServerMessageService _serverMessageService;

    public BotMessageCreateModal(ServerMessageService serverMessageService)
    {
        _serverMessageService = serverMessageService ?? throw new ArgumentNullException(nameof(serverMessageService));
    }

    public Task<Modal> BuildAsync(string customId)
    {
        if (string.IsNullOrWhiteSpace(customId))
        {
            throw new ArgumentException("Custom ID must not be empty", nameof(customId));
        }
        
        return Task.FromResult(new ModalBuilder()
            .WithTitle("Create new bot message")
            .WithCustomId(customId)
            .AddTextInput("Name", "message_name", minLength: 1, maxLength: 32)
            .AddTextInput("Message content", "message_content", TextInputStyle.Paragraph, minLength: 1, maxLength: 2000)
            .Build());
    }

    public async Task ExecuteAsync(SocketModal modalInteraction)
    {
        string name = modalInteraction.Data.Components.Single(x => x.CustomId == "message_name").Value;
        string content = modalInteraction.Data.Components.Single(x => x.CustomId == "message_content").Value;
        await _serverMessageService.CreateMessageAsync(content, name, modalInteraction.User, modalInteraction.Channel);
        await modalInteraction.RespondAsync("Bot message created.", ephemeral: true);
    }
}