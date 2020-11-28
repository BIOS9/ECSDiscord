using ComponentApplication.Components.Services;
using Discord;
using Discord.WebSocket;
using DiscordBot.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static ComponentApplication.Components.Services.IService;

namespace DiscordBot
{
    public class DiscordBot : IService
    {
        public string Name => "Discord Bot";
        public Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        private const string LoggerName = "Discord Bot";
        private const string ConfigSectionName = "DiscordBot";

        private readonly ILogger _logger;
        private readonly IStringLocalizer _localizer;
        private readonly IBotStorageProvider _botStorage;
        private readonly DiscordBotConfig _config;
        private readonly DiscordSocketClient _discordClient;
        private readonly SemaphoreSlim _clientReadyLock = new SemaphoreSlim(0);

        public DiscordBot(
            ILoggerFactory loggerFactory,
            IStringLocalizerFactory localizerFactory,
            IConfigurationRoot configurationRoot,
            IBotStorageProvider botStorageProvider)
        {
            _logger = loggerFactory.CreateLogger(LoggerName);
            _localizer = localizerFactory.Create(typeof(DiscordBot));
            _config = new DiscordBotConfig(
                configurationRoot.GetSection(ConfigSectionName),
                loggerFactory);
            _botStorage = botStorageProvider;
            _discordClient = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Debug,       // Tell the logger to give all info, log level will be handled by the BotLogger
                MessageCacheSize = 1000,             // Cache 1,000 messages per channel
                DefaultRetryMode = RetryMode.AlwaysRetry,
            });
            _discordClient.Log += new BotLogger(loggerFactory).Log;
            _discordClient.Ready += _discordClient_Ready;
        }

        private Task _discordClient_Ready()
        {
            _clientReadyLock.Release();
            return Task.CompletedTask;
        }

        public async Task StartAsync()
        {
            _logger.LogInformation(_localizer["LOG_LOGIN"]);
            await _discordClient.LoginAsync(TokenType.Bot, _config.Token); // Login to Discord
            _logger.LogInformation(_localizer["LOG_STARTING"]);
            await _discordClient.StartAsync(); // Connect to the websocket
            await _clientReadyLock.WaitAsync(); // Wait for Discord client to be ready.
            //await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);     // Load commands and modules into the command service
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(_localizer["LOG_READY"]);
            await Task.Delay(-1, cancellationToken);
        }

        public async Task StopAsync()
        {
            _logger.LogInformation(_localizer["LOG_STOPPING"]);
            await _discordClient.StopAsync();
            await _discordClient.LogoutAsync();
            _logger.LogInformation(_localizer["LOG_STOPPED"]);
        }
    }
}
