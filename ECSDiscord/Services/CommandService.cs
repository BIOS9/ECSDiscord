using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
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
            _discord = discord;
            _commands = commands;
            _config = config;
            _provider = provider;

            _discord.MessageReceived += OnMessageReceivedAsync;
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
                            await context.Channel.SendMessageAsync($"Sorry, that is an unknown command.\nTry `{_config["prefix"]}help` to see a list of commands.");
                            break;
                        case CommandError.BadArgCount:
                            await context.Channel.SendMessageAsync($"Sorry, you supplied an invalid number of arguments.\nTry `{_config["prefix"]}help <command>`");
                            break;
                        case CommandError.UnmetPrecondition:
                            if (result.ErrorReason.StartsWith("User requires guild permission"))
                                await context.Channel.SendMessageAsync("Sorry, you don't have permission to run that command.");
                            else
                                await context.Channel.SendMessageAsync("Sorry, something is preventing that command from being run.\nPlease ask an admin to check the logs.");
                            break;
                        default:
                            await context.Channel.SendMessageAsync("An error occured: " + result.ToString());
                            break;
                    }
                }
            }
        }
    }
}
