using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Config
{
    internal class DiscordBotConfig
    {
        public string Token { get; }

        private readonly ILogger _logger;

        public DiscordBotConfig(
            IConfigurationSection configurationSection,
            ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger("Discord Bot Configuration");
            var c = configurationSection;

            checkSectionExists(c);
            Token = readToken(c);
        }

        private void checkSectionExists(IConfigurationSection configurationSection)
        {
            if (!configurationSection.Exists())
            {
                _logger.LogCritical("Discord bot configuration section is missing.");
                throw new DiscordBotConfigurationException("Discord bot configuration section does not exist.");
            }
        }

        private string readToken(IConfigurationSection configurationSection)
        {
            var t = configurationSection["Token"];
            if (t == null || string.Empty.Equals(t))
                fail("Discord bot token is missing.");
            return t;
        }

        private void fail(string message)
        {
            _logger.LogCritical(message);
            throw new DiscordBotConfigurationException(message);
        }
    }
}
