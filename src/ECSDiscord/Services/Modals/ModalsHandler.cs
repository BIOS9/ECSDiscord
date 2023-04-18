using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using ECSDiscord.Services.Bot;
using ECSDiscord.Util;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ECSDiscord.Services.Modals;

public class ModalsHandler : IHostedService
{
    private readonly DiscordSocketClient _discordClient;
    private readonly ILogger<ModalsHandler> _logger;
    private readonly ConcurrentDictionary<string, IModal> _modals = new();

    public ModalsHandler(
        DiscordBot discordBot,
        ILogger<ModalsHandler> logger)
    {
        _discordClient = discordBot.DiscordClient ?? throw new ArgumentNullException(nameof(discordBot));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<Modal> RegisterModalAsync<TModal>(TModal modal) where TModal : IModal
    {
        for (int i = 0; i < 10; ++i)
        {
            string customId = modal.Name + StringExtensions.RandomString(10);
            if (_modals.TryAdd(customId, modal))
            {
                _logger.LogDebug("Registered modal: {CustomId}", customId);
                return modal.BuildAsync(customId);
            }
        }

        throw new Exception("Failed to register modal after 10 retries. Custom ID already exists.");
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
            _logger.LogInformation("Executing modal module {CustomId}", modal.CustomId);
            await modal.ExecuteAsync(modalInteraction);
            _modals.TryRemove(modalInteraction.Data.CustomId, out _);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception thrown while running modal {CustomId}. Message: {Message}",
                modal.CustomId, ex.Message);
            await modalInteraction.FollowupAsync(":fire:  A server error occured while running this command!", ephemeral: true);
        }
    }
}