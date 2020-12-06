using ComponentApplication.Components.Services;
using DiscordBot.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
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

        public async Task StartAsync()
        {
            try
            {
                _logger.LogDebug("Testing MySql connection.");
                using (MySqlConnection con = GetMySqlConnection())
                {
                    await con.OpenAsync();
                    _logger.LogInformation("MySql server version: {version}", con.ServerVersion);
                }
                _logger.LogDebug("MySql connection test succeeded.");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to connect to MySql {message}", ex.Message);
                throw ex;
            }
        }

        public Task RunAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            return Task.CompletedTask;
        }

        protected MySqlConnection GetMySqlConnection()
        {
            return new MySqlConnection(_config.ConnectionString);
        }

        public Task<string> GetCommandPrefixAsync(ulong guildID)
        {
            return Task.FromResult("}");
        }
    }
}
