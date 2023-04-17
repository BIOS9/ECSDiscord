using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ECSDiscord.Services.Bot
{
    public class DiscordBot : IHostedService
    {
        public DiscordSocketClient DiscordClient { get; }

        private readonly DiscordBotOptions _options;
        private readonly ILogger<DiscordSocketClient> _botLogger;
        private readonly ILogger<DiscordBot> _logger;

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

        public DiscordBot(DiscordBotOptions options, ILoggerFactory loggerFactory)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }
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
            _botLogger.Log(level, arg.Exception, "{Source} {Message}", arg.Source, arg.Message);
            return Task.CompletedTask;
        }

        private async Task _discord_GuildAvailable(SocketGuild arg)
        {
            if (arg.Id != _options.GuildId)
            {
                Log.Warning("Leaving guild {Guild} Config guildId does not match", arg.Name);
                await arg.LeaveAsync();
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Discord bot");
            DiscordClient.Log += DiscordClient_Log;
            DiscordClient.GuildAvailable += _discord_GuildAvailable;
            await DiscordClient.SetActivityAsync(new Game(_options.StatusText));
            await DiscordClient.LoginAsync(TokenType.Bot, _options.Token);
            await DiscordClient.StartAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Discord bot");
            await DiscordClient.StopAsync();
            DiscordClient.GuildAvailable -= _discord_GuildAvailable;
            DiscordClient.Log += DiscordClient_Log;
        }
    }
}
