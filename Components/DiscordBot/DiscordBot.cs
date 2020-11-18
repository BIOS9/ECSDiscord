using ComponentApplication.Components.Services;
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

        public DiscordBot(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger("Discord Bot");
        }

        public async Task StartAsync()
        {
            State = ServiceState.Starting;
            _logger.LogInformation("Started!");
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
