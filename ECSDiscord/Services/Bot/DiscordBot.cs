using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ECSDiscord.Services.Bot
{
    public class DiscordBot : IHostedService
    {
        public readonly DiscordSocketClient DiscordClient;

        private readonly ILogger<DiscordSocketClient> _botLogger;
        private readonly ILogger<DiscordBot> _logger;
        private readonly IConfiguration _config;

        private bool _dmOnJoin;
        private string
            _joinDmTemplate,
            _prefix;

        private readonly IReadOnlyDictionary<LogSeverity, LogLevel> _logLevelMap = // Maps Discord.NET logging levels to Microsoft extensions logging levels.
            new Dictionary<LogSeverity, LogLevel>
            {
                { LogSeverity.Debug, LogLevel.Trace },
                { LogSeverity.Verbose, LogLevel.Debug },
                { LogSeverity.Info, LogLevel.Information },
                { LogSeverity.Warning, LogLevel.Warning },
                { LogSeverity.Error, LogLevel.Error },
                { LogSeverity.Critical, LogLevel.Critical }
            };

        public DiscordBot(IConfiguration config, ILoggerFactory loggerFactory)
        {
            _config = config;

            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<DiscordBot>();
            _botLogger = loggerFactory.CreateLogger<DiscordSocketClient>();

            DiscordClient = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                LogGatewayIntentWarnings = true,
                AlwaysDownloadUsers = true,
                MessageCacheSize = 1000,
                GatewayIntents = GatewayIntents.Guilds
                             | GatewayIntents.MessageContent
                             | GatewayIntents.DirectMessages
                             | GatewayIntents.GuildMembers
                             | GatewayIntents.GuildMessages
                             | GatewayIntents.GuildMessageReactions
            });

        }

        private Task DiscordClient_Log(LogMessage arg)
        {
            var level = _logLevelMap[arg.Severity];
            _botLogger.Log(level, arg.Exception, "{source} {message}", arg.Source, arg.Message);
            return Task.CompletedTask;
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

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Discord bot");
            DiscordClient.Log += DiscordClient_Log;
            DiscordClient.GuildAvailable += _discord_GuildAvailable;
            DiscordClient.UserJoined += _discord_UserJoined;
            loadConfig();
            string discordToken = _config["secrets:discordBotToken"];     // Get the discord token from the config file
            if (string.IsNullOrWhiteSpace(discordToken))
            {
                Log.Fatal($"Cannot find bot token in configuration file. Exiting...");
                throw new Exception("Bot token not found in configuration file.");
            }

            await DiscordClient.SetActivityAsync(new Game("github.com/BIOS9/ECSDiscord"));
            await DiscordClient.LoginAsync(TokenType.Bot, discordToken);     // Login to discord
            await DiscordClient.StartAsync();                               // Connect to the websocket
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Discord bot");
            await DiscordClient.StopAsync();
            DiscordClient.GuildAvailable -= _discord_GuildAvailable;
            DiscordClient.UserJoined -= _discord_UserJoined;
            DiscordClient.Log += DiscordClient_Log;
        }
    }
}
