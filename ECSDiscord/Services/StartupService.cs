using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace ECSDiscord.Services
{
    public class StartupService
    {
        private readonly IServiceProvider _provider;
        private readonly DiscordSocketClient _discord;
        private readonly Discord.Commands.CommandService _commands;
        private readonly IConfigurationRoot _config;

        private bool _dmOnJoin;
        private string
            _joinDmTemplate,
            _prefix;

        // DiscordSocketClient, CommandService, and IConfigurationRoot are injected automatically from the IServiceProvider
        public StartupService(
            IServiceProvider provider,
            DiscordSocketClient discord,
            Discord.Commands.CommandService commands,
            IConfigurationRoot config)
        {
            Log.Debug("Startup service loading.");
            _provider = provider;
            _config = config;
            _discord = discord;
            _commands = commands;
            _discord.UserJoined += _discord_UserJoined;
            loadConfig();
            Log.Debug("Startup service loaded.");
        }

        private async Task _discord_UserJoined(SocketGuildUser arg)
        {
            try
            {
                Log.Debug("User joined {user}. DM on join is set to {dmOnJoin}", arg.Id, _dmOnJoin);
                if (_dmOnJoin)
                {
                    Log.Debug("Sending join DM to {user}", arg.Id);
                    await arg.SendMessageAsync(fillJoinDmTemplate(_joinDmTemplate));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to message user {user} on join {message}", arg.Id, ex.Message);
            }
        }

        private async void startConnectionWatchdogAsync()
        {
            Log.Information("Watchdog started.");
            string discordToken = _config["secrets:discordBotToken"];     // Get the discord token from the config file
            while (true)
            {
                await Task.Delay(5000);
                try
                {
                    if (_discord.ConnectionState != ConnectionState.Connected)
                    {
                        await Task.Delay(10000);
                        Log.Information("Connection watchdog attempting connection...");
                        await _discord.LoginAsync(TokenType.Bot, discordToken);     // Login to discord
                        await _discord.StartAsync();                               // Connect to the websocket
                        await Task.Delay(20000);
                    }
                }
                catch(Exception ex)
                {
                    Log.Error(ex, "Connection watchdog error: " + ex.Message);
                    await Task.Delay(120000);
                }
            }
        }

        /// <summary>
        /// Start the service and add 
        /// </summary>
        /// <returns></returns>
        public async Task StartAsync()
        {
            string discordToken = _config["secrets:discordBotToken"];     // Get the discord token from the config file
            if (string.IsNullOrWhiteSpace(discordToken))
            {
                Log.Fatal($"Cannot find bot token in configuration file. Exiting...");
                throw new Exception("Bot token not found in configuration file.");
            }

            _discord.GuildAvailable += _discord_GuildAvailable;
            await _discord.SetActivityAsync(new Game("nightfish.co/wgtn"));
            startConnectionWatchdogAsync();

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);     // Load commands and modules into the command service

            await _discord.LoginAsync(TokenType.Bot, discordToken);     // Login to discord
            await _discord.StartAsync();                               // Connect to the websocket
        }

        private async Task _discord_GuildAvailable(SocketGuild arg)
        {
            if (!ulong.TryParse(_config["guildId"], out ulong guildId))
            {
                Log.Warning("guildId in configuration is invalid. Expected unsigned long integer, got: {id}", _config["guildId"]);
                return;
            }

            if (arg.Id != guildId)
            {
                Log.Warning("Leaving guild {guild} Config guildId does not match.", arg.Name);
                await arg.LeaveAsync();
            }
        }

        private string fillJoinDmTemplate(string template)
        {
            return template.Replace("{prefix}", _prefix).Trim();
        }

        private void loadConfig()
        {
            if (!bool.TryParse(_config["dmUsersOnJoin"], out _dmOnJoin))
            {
                Log.Error("Invalid boolean value for dmUsersOnJoin in config.");
                throw new ArgumentException("Invalid boolean value for dmUsersOnJoin in config.");
            }
            _joinDmTemplate = _config["joinDmTemplate"];
            if (string.IsNullOrWhiteSpace(_joinDmTemplate) && _dmOnJoin)
            {
                _dmOnJoin = false;
                Log.Warning("DM on join is enabled, but the DM template is empty. DM on join is now disabled.");
            }
            _prefix = _config["prefix"];
        }
    }
}
