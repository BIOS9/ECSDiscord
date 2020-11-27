using ComponentApplication.Components.Services;
using DiscordBot.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
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
        }

        public async Task StartAsync()
        {
            State = ServiceState.Starting;
            _logger.LogInformation("Started!");
            _logger.LogInformation(_localizer["TEST"]);
            await Task.Delay(5000);
            State = ServiceState.Running;
        }

        public async Task StopAsync()
        {
            State = ServiceState.Stopping;
            _logger.LogInformation("Stopped!");
            await Task.Delay(5000);
            State = ServiceState.Stopped;
        }
    }
}
