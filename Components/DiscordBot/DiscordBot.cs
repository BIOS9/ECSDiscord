using ComponentApplication.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace DiscordBotComponent
{
    public class DiscordBot : IComponent
    {
        public string Name => "Discord Bot";
        public Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        private ILogger _logger;

        public DiscordBot(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger("Discord Bot");
        }

        public Task StartAsync()
        {
            _logger.LogInformation("Started!");
            return Task.Delay(5000);
        }

        public Task StopAsync()
        {
            _logger.LogInformation("Stopped!");
            return Task.Delay(5000);
        }
    }
}
