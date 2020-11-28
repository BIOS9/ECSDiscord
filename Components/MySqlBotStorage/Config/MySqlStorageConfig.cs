using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MySqlBotStorage.Config
{
    internal class MySqlStorageConfig
    {
        public string ConnectionString { get; }

        private readonly ILogger _logger;

        public MySqlStorageConfig(
            IConfigurationSection configurationSection,
            ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger("MySql Storage Configuration");
            var c = configurationSection;

            checkSectionExists(c);
            ConnectionString = readConnectionString(c);
        }

        private void checkSectionExists(IConfigurationSection configurationSection)
        {
            if (!configurationSection.Exists())
                fail("MySql storage configuration section is missing or empty.");
        }

        private string readConnectionString(IConfigurationSection configurationSection)
        {
            var t = configurationSection["ConnectionString"];
            if (t == null || string.Empty.Equals(t))
                fail("MySql connection string is missing.");
            return t;
        }

        private void fail(string message)
        {
            _logger.LogCritical(message);
            throw new ConfigException(message);
        }
    }
}
