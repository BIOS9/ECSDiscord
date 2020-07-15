using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ECSDiscord.Services
{
    public class StartupService
    {
        private readonly IServiceProvider _provider;
        private readonly DiscordSocketClient _discord;
        private readonly Discord.Commands.CommandService _commands;
        private readonly IConfigurationRoot _config;

        // DiscordSocketClient, CommandService, and IConfigurationRoot are injected automatically from the IServiceProvider
        public StartupService(
            IServiceProvider provider,
            DiscordSocketClient discord,
            Discord.Commands.CommandService commands,
            IConfigurationRoot config)
        {
            _provider = provider;
            _config = config;
            _discord = discord;
            _commands = commands;
        }

        public async Task StartAsync()
        {
            string discordToken = _config["secrets:discordBotToken"];     // Get the discord token from the config file
            if (string.IsNullOrWhiteSpace(discordToken))
                throw new Exception("Please enter bot token into the configuration file.");

            await _discord.LoginAsync(TokenType.Bot, discordToken);     // Login to discord
            await _discord.StartAsync();                                // Connect to the websocket

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);     // Load commands and modules into the command service
        }
    }
}
