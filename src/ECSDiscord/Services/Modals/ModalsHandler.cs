using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using ECSDiscord.Services.Bot;
using ECSDiscord.Services.ModerationLog;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ECSDiscord.Services.Modals;

public class ModalsHandler : IHostedService
{
    private readonly DiscordSocketClient _discordClient;
    private readonly ILogger<ModalsHandler> _logger;
    private readonly Dictionary<string, IModal> _modals;

    public ModalsHandler(
        DiscordBot discordBot,
        ILogger<ModalsHandler> logger,
        IEnumerable<IModal> modals)
    {
        _discordClient = discordBot.DiscordClient ?? throw new ArgumentNullException(nameof(discordBot));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(modals, nameof(modals));
        _modals = modals.ToDictionary(x => x.CustomId, x => x);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Hooking modal events...");
        _discordClient.ModalSubmitted += DiscordClientOnModalSubmitted;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Unhooking modal events...");
        _discordClient.ModalSubmitted -= DiscordClientOnModalSubmitted;
        return Task.CompletedTask;
    }

    private Task DiscordClientOnModalSubmitted(SocketModal modalInteraction)
    {
        _logger.LogDebug("Modal submitted event received {ModalId}", modalInteraction.Data.CustomId);
        
        if (!_modals.TryGetValue(modalInteraction.Data.CustomId, out var modal))
        {
            _logger.LogError("Unhandled modal executed {Modal}", modalInteraction.Data.CustomId);
            return Task.CompletedTask;
        }

        _ = RunModal(modal, modalInteraction);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Runs modal with exception handling.
    /// </summary>
    private async Task RunModal(IModal modal, SocketModal modalInteraction)
    {
        try
        {
            _logger.LogInformation("Executing modal module {Modal}", modal.CustomId);
            await modal.ExecuteAsync(modalInteraction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception thrown while running modal {Modal}. Message: {Message}",
                modal.CustomId, ex.Message);
            await modalInteraction.FollowupAsync(":fire:  A server error occured while running this command!", ephemeral: true);
        }
    }
}