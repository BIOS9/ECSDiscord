﻿using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace ECSDiscord.Services
{
    public class CommandService
    {
        private readonly DiscordSocketClient _discord;
        private readonly Discord.Commands.CommandService _commands;
        private readonly IConfigurationRoot _config;
        private readonly IServiceProvider _provider;

        // DiscordSocketClient, CommandService, IConfigurationRoot, and IServiceProvider are injected automatically from the IServiceProvider
        public CommandService(
            DiscordSocketClient discord,
            Discord.Commands.CommandService commands,
            IConfigurationRoot config,
            IServiceProvider provider)
        {
            Log.Debug("Command service loading.");
            _discord = discord;
            _commands = commands;
            _config = config;
            _provider = provider;

            _commands.AddModulesAsync(assembly: Assembly.GetExecutingAssembly(), provider);
            _discord.MessageReceived += OnMessageReceivedAsync;
            Log.Debug("Command service loaded.");
        }

        private async Task OnMessageReceivedAsync(SocketMessage s)
        {
            var msg = s as SocketUserMessage; // Ensure the message is from a user/bot
            if (msg == null) return;
            if (msg.Author.Id == _discord.CurrentUser.Id) return; // Ignore self when checking commands

            var context = new SocketCommandContext(_discord, msg); // Create the command context

            int argPos = 0; // Check if the message has a valid command prefix
            if (msg.HasStringPrefix(_config["prefix"], ref argPos) || msg.HasMentionPrefix(_discord.CurrentUser, ref argPos))
            {
                var result = await _commands.ExecuteAsync(context, argPos, _provider); // Execute the command

                if (!result.IsSuccess) // If not successful, reply with the error.
                {
                    switch (result.Error)
                    {
                        case CommandError.UnknownCommand:
                            string replyChannel = _config["forcedChannels:unknownCommandReply"];
                            if (replyChannel != null && ulong.Parse(replyChannel) != s.Channel.Id)
                                return;
                            Log.Debug("User {discordUser} sent unknown command.", s.Author.Id);
                            await context.Channel.SendMessageAsync($"Sorry, that is an unknown command.\nTry `{_config["prefix"]}help` to see a list of commands.");
                            break;
                        case CommandError.BadArgCount:
                            Log.Debug("User {discordUser} sent command with invalid number of arguments.", s.Author.Id);
                            await context.Channel.SendMessageAsync($"Sorry, you supplied an invalid number of arguments.\nTry `{_config["prefix"]}help <command>`");
                            break;
                        case CommandError.UnmetPrecondition:
                            if (result.ErrorReason.StartsWith("User requires guild permission"))
                            {
                                Log.Debug("User {discordUser} tried to execute command without the correct permissions.", s.Author.Id);
                                await context.Channel.SendMessageAsync("Sorry, you don't have permission to run that command.");
                            }
                            else if (result.ErrorReason.StartsWith("Command must be used in a guild channel."))
                            {
                                Log.Debug("User {discordUser} tried to execute command that requires guild channel in private message.", s.Author.Id);
                                await context.Channel.SendMessageAsync("Sorry, that command can only be run in a Discord Server channel.");
                            }
                            else
                            {
                                Log.Warning("Error while executing command for user {discordId} {error}", s.Author.Id, result.ErrorReason);
                                await context.Channel.SendMessageAsync("Sorry, something is preventing that command from being run.\nPlease ask an admin to check the logs.");
                            }
                            break;
                        default:
                            Log.Warning("Error while executing command for user {discordId} {error}", s.Author.Id, result.ErrorReason);
                            await context.Channel.SendMessageAsync("An error occured: " + result.ToString());
                            break;
                    }
                }
            }
        }
    }
}
