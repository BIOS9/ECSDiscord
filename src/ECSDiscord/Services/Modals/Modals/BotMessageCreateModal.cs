using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace ECSDiscord.Services.Modals.Modals;

public class BotMessageCreateModal : IModal
{
    private static string Name => "bot_message_create";
    public string CustomId => Name;

    private readonly ServerMessageService _serverMessageService;

    public BotMessageCreateModal(ServerMessageService serverMessageService)
    {
        _serverMessageService = serverMessageService ?? throw new ArgumentNullException(nameof(serverMessageService));
    }

    public static Modal Build()
    {
        return new ModalBuilder()
            .WithTitle("Create new bot message")
            .WithCustomId(Name)
            .AddTextInput("Name", "message_name", minLength: 1, maxLength: 32)
            .AddTextInput("Message content", "message_content", TextInputStyle.Paragraph, minLength: 1, maxLength: 2000)
            .Build();
    }

    public async Task ExecuteAsync(SocketModal modalInteraction)
    {
        string name = modalInteraction.Data.Components.Single(x => x.CustomId == "message_name").Value;
        string content = modalInteraction.Data.Components.Single(x => x.CustomId == "message_content").Value;
        await _serverMessageService.CreateMessageAsync(content, name, modalInteraction.User, modalInteraction.Channel);
        await modalInteraction.RespondAsync("Bot message created.", ephemeral: true);
    }
}