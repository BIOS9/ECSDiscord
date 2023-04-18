using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace ECSDiscord.Services.Modals.Modals;

public class BotMessageEditModal : IModal
{
    public string Name => "bot_message_edit";
    public string CustomId { get; set; }

    private readonly ServerMessageService _serverMessageService;
    private readonly ulong _messageId;
    
    public BotMessageEditModal(ulong messageId, ServerMessageService serverMessageService)
    {
        _messageId = messageId;
        _serverMessageService = serverMessageService ?? throw new ArgumentNullException(nameof(serverMessageService));
    }

    public async Task<Modal> BuildAsync(string customId)
    {
        var existingMessage = await _serverMessageService.GetMessageAsync(_messageId);
        return new ModalBuilder()
            .WithTitle("Edit bot message")
            .WithCustomId(customId)
            .AddTextInput("Message name", "message_name", TextInputStyle.Short, value: existingMessage.Name, minLength: 1, maxLength: 2000)
            .AddTextInput("Message content", "message_content", TextInputStyle.Paragraph, value: existingMessage.Content, minLength: 1, maxLength: 2000)
            .Build();
    }

    public async Task ExecuteAsync(SocketModal modalInteraction)
    {
        string name = modalInteraction.Data.Components.Single(x => x.CustomId == "message_name").Value;
        string content = modalInteraction.Data.Components.Single(x => x.CustomId == "message_content").Value;
        await _serverMessageService.EditMessageAsync(_messageId, content, name, modalInteraction.User);
        await modalInteraction.RespondAsync("Bot message edited.", ephemeral: true);
    }
}