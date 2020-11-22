using ComponentApplication.Components.Services;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading.Tasks;
using static ComponentApplication.Components.Services.IService;

namespace DiscordBotComponent
{
    public class DiscordBot : IService
    {
        public string Name => "Discord Bot";
        public Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public ServiceState State { get; private set; }

        private ILogger _logger;
        private IStringLocalizer _localizer;

        public DiscordBot(ILoggerFactory loggerFactory, IStringLocalizerFactory localizerFactory)
        {
            _logger = loggerFactory.CreateLogger("Discord Bot");
            _localizer = localizerFactory.Create(typeof(DiscordBot));
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
