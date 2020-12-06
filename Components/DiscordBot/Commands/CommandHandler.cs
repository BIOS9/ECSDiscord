using ComponentApplication.Components.Services;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Storage;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordBot.Commands
{
    internal class CommandHandler : IDisposable
    {
        private const string LoggerName = "Command Handler";

        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IBotStorageProvider _botStorage;
        private readonly IStringLocalizer _localizer;
        private readonly DiscordSocketClient _discordClient;
        private readonly CommandService _commandService;

        public CommandHandler(
            ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider,
            IBotStorageProvider botStorage,
            IStringLocalizer localizer,
            CommandService commandService,
            DiscordSocketClient discordClient)
        {
            _logger = loggerFactory.CreateLogger(LoggerName);
            _serviceProvider = serviceProvider;
            _botStorage = botStorage;
            _localizer = localizer;
            _discordClient = discordClient;
            _commandService = commandService;

            _discordClient.MessageReceived += _discordClient_MessageReceived;
            _commandService.Log += new BotLogger(loggerFactory, "Command Service").Log;
        }

        private async Task _discordClient_MessageReceived(SocketMessage sockMsg)
        {
            _logger.LogTrace("Received command message \"{message}\"", sockMsg.Content);
            var msg = sockMsg as SocketUserMessage; // Ensure the message is from a user/bot.
            if (msg == null) return;
            if (msg.Author.Id == _discordClient.CurrentUser.Id) return; // Ignore self when checking commands.

            var context = new SocketCommandContext(_discordClient, msg); // Create the command context.

            string channelMention = string.Empty;
            bool valid = false;
            int argPos = 0;
            if (context.Guild != null) // If message was sent in a guild.
            {
                string prefix = await _botStorage.GetCommandPrefixAsync(context.Guild.Id); // Get prefix for server from storage.
                if (msg.HasStringPrefix(prefix, ref argPos)) // Check if command begins with prefix.
                    valid = true;
                channelMention = sockMsg.Author.Mention; // Mention user when command is used in a guild.
            }
            else
            {
                valid = true;
            }

            if (valid == false && msg.HasMentionPrefix(_discordClient.CurrentUser, ref argPos)) // If bot was mentioned.
                valid = true;

            if (!valid)
                return;

            _logger.LogTrace("Executing command \"{command}\" for {user}#{discriminator} {userid}",
                sockMsg.Content,
                sockMsg.Author.Username,
                sockMsg.Author.Discriminator,
                sockMsg.Author.Id);
            var result = await _commandService.ExecuteAsync(context, argPos, _serviceProvider); // Execute the command

            if (result.IsSuccess) // If not successful, reply with the error.
            {
                _logger.LogDebug("{user}#{discriminator} {userid} executed command \"{command}\"",
                   sockMsg.Author.Username,
                   sockMsg.Author.Discriminator,
                   sockMsg.Author.Id,
                   sockMsg.Content);
                return;
            }

            switch (result.Error)
            {
                case CommandError.UnknownCommand:
                    _logger.LogInformation("{user}#{discriminator} {userid} executed unknown command \"{command}\"",
                        sockMsg.Author.Username,
                        sockMsg.Author.Discriminator,
                        sockMsg.Author.Id,
                        sockMsg.Content);

                    await context.Channel.SendMessageAsync(_localizer["UNKNOWN_COMMAND", channelMention]);
                    break;
                case CommandError.BadArgCount:
                    _logger.LogInformation("{user}#{discriminator} {userid} executed \"{command}\" but supplied an invalid number of arguments.",
                            sockMsg.Author.Username,
                            sockMsg.Author.Discriminator,
                            sockMsg.Author.Id,
                            sockMsg.Content);
                    await context.Channel.SendMessageAsync(_localizer["COMMAND_BAD_ARG_COUNT", channelMention]);
                    break;
                case CommandError.UnmetPrecondition:
                    if (result.ErrorReason.StartsWith("User requires guild permission"))
                    {
                        _logger.LogInformation("{user}#{discriminator} {userid} attempted to execute \"{command}\" without permission.",
                            sockMsg.Author.Username,
                            sockMsg.Author.Discriminator,
                            sockMsg.Author.Id,
                            sockMsg.Content);
                        await context.Channel.SendMessageAsync(_localizer["COMMAND_PERMISSION_DENIED", channelMention]);
                    }
                    else if (result.ErrorReason.StartsWith("Command must be used in a guild channel."))
                    {
                        _logger.LogInformation("{user}#{discriminator} {userid} attempted to execute server command \"{command}\" in direct messages.",
                            sockMsg.Author.Username,
                            sockMsg.Author.Discriminator,
                            sockMsg.Author.Id,
                            sockMsg.Content);
                        await context.Channel.SendMessageAsync(_localizer["GUILD_ONLY_COMMAND", channelMention]);
                    }
                    else
                    {
                        _logger.LogWarning("{user}#{discriminator} {userid} attempted to execute \"{command}\" with an unmet precondition \"{reason}\".",
                            sockMsg.Author.Username,
                            sockMsg.Author.Discriminator,
                            sockMsg.Author.Id,
                            sockMsg.Content,
                            result.ErrorReason);
                        await context.Channel.SendMessageAsync(_localizer["UNMET_COMMAND_PRECONDITION", channelMention]);
                    }
                    break;
                default:
                    _logger.LogError("{user}#{discriminator} {userid} executed \"{command}\" but an error occured \"{reason}\".",
                            sockMsg.Author.Username,
                            sockMsg.Author.Discriminator,
                            sockMsg.Author.Id,
                            sockMsg.Content,
                            result.ErrorReason);
                    await context.Channel.SendMessageAsync(_localizer["COMMAND_ERROR", channelMention]);
                    break;
            }
        }

        public async Task LoadCommandsAsync()
        {
            _logger.LogDebug("Loading commands...");
            var modules = await _commandService.AddModulesAsync(Assembly.GetExecutingAssembly(), _serviceProvider); // Load commands and modules into the command service
            foreach (var module in modules)
            {
                _logger.LogDebug("Loaded command module: {module}", module.Name);
            }
        }

        public void Dispose()
        {
            _discordClient.MessageReceived -= _discordClient_MessageReceived;
        }
    }
}
