using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using ECSDiscord.Core.Translations;
using Microsoft.Extensions.Hosting;
using System.Threading;
using ECSDiscord.Services.Bot;

namespace ECSDiscord.Services.PrefixCommands
{
    public class PrefixCommandsHandler : IHostedService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IConfiguration _config;
        private readonly IServiceProvider _provider;
        private readonly ITranslator _translator;

        // DiscordSocketClient, CommandService, IConfigurationRoot, and IServiceProvider are injected automatically from the IServiceProvider
        public PrefixCommandsHandler(
            DiscordBot discordBot,
            CommandService commands,
            IConfiguration config,
            IServiceProvider provider,
            ITranslator translator)
        {
            Log.Debug("Command service loading.");
            _discord = discordBot.DiscordClient;
            _commands = commands;
            _config = config;
            _provider = provider;
            _translator = translator;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Log.Debug("Loading command service");
            _commands.AddModulesAsync(assembly: Assembly.GetExecutingAssembly(), _provider);
            _discord.MessageReceived += OnMessageReceivedAsync;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            //_commands.RemoveModulesAsync(assembly: Assembly.GetExecutingAssembly(), _provider); // Will remove this when switching to slash commands
            _discord.MessageReceived -= OnMessageReceivedAsync;
            return Task.CompletedTask;
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
                if (msg.Content.ToLower().Equals("+vewify"))
                {
                    await msg.ReplyAsync("Hewwo and wewcome uwu to teh ecs discowd sewvew.\nI've sent chu a dm wif fuwthew instwuctions on how uwu to vewify. :pleading_face: <:awooo:958999403975290970>");
                    return;
                }

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
