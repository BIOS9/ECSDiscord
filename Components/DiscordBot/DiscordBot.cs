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

        public Task LoadAsync()
        {
            _logger.LogInformation("Loaded!");
            return Task.CompletedTask;
        }

        public Task UnloadAsync()
        {
            _logger.LogInformation("Unloaded!");
            return Task.CompletedTask;
        }
    }
}
