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
        public ServiceState State { get; private set; }

        private const string LoggerName = "Discord Bot";
        private const string ConfigSectionName = "DiscordBot";

        private readonly ILogger _logger;
        private readonly IStringLocalizer _localizer;
        private readonly DiscordBotConfig _config;
        private readonly DiscordSocketClient _discordClient;

        public DiscordBot(
            ILoggerFactory loggerFactory,
            IStringLocalizerFactory localizerFactory,
            IConfigurationRoot configurationRoot)
        {
            _logger = loggerFactory.CreateLogger(LoggerName);
            _localizer = localizerFactory.Create(typeof(DiscordBot));
            _config = new DiscordBotConfig(
                configurationRoot.GetSection(ConfigSectionName),
                loggerFactory);
            _discordClient = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Debug,       // Tell the logger to give all info, log level will be handled by the BotLogger
                MessageCacheSize = 1000,             // Cache 1,000 messages per channel
                DefaultRetryMode = RetryMode.AlwaysRetry,
            });
            _discordClient.Log += new BotLogger(loggerFactory).Log;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            State = ServiceState.Starting;
            _logger.LogInformation(_localizer["LOG_LOGIN"]);
            await _discordClient.LoginAsync(TokenType.Bot, _config.Token); // Login to Discord
            _logger.LogInformation(_localizer["LOG_STARTING"]);
            await _discordClient.StartAsync(); // Connect to the websocket
            _logger.LogInformation(_localizer["LOG_STARTED"]);

            //await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);     // Load commands and modules into the command service
            State = ServiceState.Running;
            await Task.Delay(-1, cancellationToken);
        }

        public async Task StopAsync()
        {
            State = ServiceState.Stopping;
            _logger.LogInformation(_localizer["LOG_STOPPING"]);
            await _discordClient.StopAsync();
            await _discordClient.LogoutAsync();
            _logger.LogInformation(_localizer["LOG_STOPPED"]);
            await Task.Delay(500);
            State = ServiceState.Stopped;
        }
    }
}
