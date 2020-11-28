using ComponentApplication.Components.Services;
using DiscordBot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySqlBotStorage.Config;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static ComponentApplication.Components.Services.IService;

namespace MySqlBotStorage
{
    internal class MySqlBotStorage : IService, IBotStorageProvider
    {
        public string Name => "MySql Bot Storage";
        public Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        private const string LoggerName = "MySql Storage";
        private const string ConfigSectionName = "MySql";

        private readonly ILogger _logger;
        private readonly MySqlStorageConfig _config;

        public MySqlBotStorage(ILoggerFactory loggerFactory, IConfigurationRoot configurationRoot)
        {
            _logger = loggerFactory.CreateLogger(LoggerName);
            _config = new MySqlStorageConfig(
                configurationRoot.GetSection(ConfigSectionName), 
                loggerFactory);
        }

        public Task StartAsync()
        {
            return Task.CompletedTask;
        }

        public Task RunAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            return Task.CompletedTask;
        }
    }
}
