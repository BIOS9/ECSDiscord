﻿using Discord.Net;
using Discord.WebSocket;
using Discord;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ECSDiscord.Services.SlashCommands;

public class SlashCommandHandler : IHostedService
{
    private readonly DiscordSocketClient _discordClient;
    private readonly ILogger<SlashCommandHandler> _logger;
    private readonly IEnumerable<ISlashCommand> _commands;

    private readonly Dictionary<ulong, ISlashCommand>
        _registeredCommands = new(); // I am aware that this is still mutable.

    public SlashCommandHandler(
        DiscordSocketClient discordBot,
        ILogger<SlashCommandHandler> logger,
        IEnumerable<ISlashCommand> commands)
    {
        _discordClient = discordBot ?? throw new ArgumentNullException(nameof(discordBot));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _commands = commands ?? throw new ArgumentNullException(nameof(commands));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Hooking slash command events...");
        _discordClient.Ready += DiscordClientOnReady;
        _discordClient.SlashCommandExecuted += DiscordClientOnSlashCommandExecuted;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Unhooking slash command events...");
        _discordClient.Ready -= DiscordClientOnReady;
        _discordClient.SlashCommandExecuted -= DiscordClientOnSlashCommandExecuted;
        return Task.CompletedTask;
    }

    private async Task DiscordClientOnReady()
    {
        try
        {
            _logger.LogDebug("Registering slash commands with Discord...");
            _registeredCommands.Clear();
            foreach (var command in _commands)
            {
                _logger.LogTrace("Registering slash command {command}", command.Name);
                SocketApplicationCommand result =
                    await _discordClient.CreateGlobalApplicationCommandAsync(command.Build());
                _registeredCommands.Add(result.Id, command);
                _logger.LogInformation("Successfully registered slash command {command}. Discord ID {id}", command.Name,
                    result.Id);
            }

            _logger.LogTrace("All slash commands registered.");
            // Using the ready event is a simple implementation for the sake of the example. Suitable for testing and development.
            // For a production bot, it is recommended to only run the CreateGlobalApplicationCommandAsync() once for each command.
        }
        catch (HttpException ex)
        {
            // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
            var json = JsonConvert.SerializeObject(ex.Errors, Formatting.Indented);
            _logger.LogError(ex, "Error registering slash commands with Discord {message} {json}", ex.Message, json);
        }
    }

    private Task DiscordClientOnSlashCommandExecuted(ISlashCommandInteraction command)
    {
        _logger.LogTrace("Slash command received {command}", command.Data.Name);
        if (!_registeredCommands.TryGetValue(command.Data.Id, out ISlashCommand commandModule))
        {
            _logger.LogError("Unhandled command executed {name} {id}", command.Data.Name, command.Data.Id);
            return Task.CompletedTask;
        }

        RunSlashCommand(commandModule, command);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Runs slash command with exception handling.
    /// </summary>
    private async void RunSlashCommand(ISlashCommand commandModule, ISlashCommandInteraction command)
    {
        try
        {
            _logger.LogInformation("Executing slash command module {command}", commandModule.Name);
            await commandModule.ExecuteAsync(command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception thrown while running slash command {command}. Message: {message}",
                command.Data.Name, ex.Message);
            await command.FollowupAsync(":fire:  A server error occured while running this command!", ephemeral: true);
        }
    }
}