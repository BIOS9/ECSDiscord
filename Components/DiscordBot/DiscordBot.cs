using Autofac;
using ComponentApplication.Components.Services;
using Discord;
using Discord.WebSocket;
using DiscordBot.Commands;
using DiscordBot.Config;
using DiscordBot.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordBot
{
    public class DiscordBot : IService, IDisposable
    {
        public string Name => "Discord Bot";
        public Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        private const string LoggerName = "Discord Bot";
        private const string ConfigSectionName = "DiscordBot";

        private readonly ILogger _logger;
        private readonly IStringLocalizer _localizer;
        private readonly IBotStorageProvider _botStorage;
        private readonly ILifetimeScope _dependencyScope;
        private readonly DiscordBotConfig _config;
        private readonly DiscordSocketClient _discordClient;
        private readonly CommandHandler _commandHandler;
        private readonly SemaphoreSlim _clientReadyLock = new SemaphoreSlim(0, 1);

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

            var builder = DependencyContainerConfig.CreateBuilder();
            builder.RegisterInstance(loggerFactory);
            builder.RegisterInstance(_localizer);
            builder.RegisterInstance(_config);
            builder.RegisterInstance(_botStorage);

            _dependencyScope = builder.Build().BeginLifetimeScope();
            _discordClient = _dependencyScope.Resolve<DiscordSocketClient>();
            _commandHandler = _dependencyScope.Resolve<CommandHandler>();

            _discordClient.Log += new BotLogger(loggerFactory, "Discord Client").Log;
            _discordClient.Ready += _discordClient_Ready;
            _discordClient.Disconnected += _discordClient_Disconnected;
            _discordClient.GuildUnavailable += _discordClient_GuildUnavailable;
        }

        private Task _discordClient_GuildUnavailable(SocketGuild arg)
        {
            _logger.LogDebug("Guild unavaiable: {guild}", arg.Name);
            return Task.CompletedTask;
        }

        private Task _discordClient_Disconnected(Exception arg)
        {
            _logger.LogDebug(arg, "Disconnected: {message}", arg.Message);
            return Task.CompletedTask;
        }

        private Task _discordClient_Ready()
        {
            _clientReadyLock.Release();
            return Task.CompletedTask;
        }

        public async Task StartAsync()
        {
            await _commandHandler.LoadCommandsAsync();
            _logger.LogInformation(_localizer["LOG_LOGIN"]);
            await _discordClient.LoginAsync(TokenType.Bot, _config.Token); // Login to Discord
            _logger.LogInformation(_localizer["LOG_STARTING"]);
            await _discordClient.StartAsync(); // Connect to the websocket
            await _clientReadyLock.WaitAsync(); // Wait for Discord client to be ready.
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

        public void Dispose()
        {
            _dependencyScope.Dispose();
        }
    }
}
