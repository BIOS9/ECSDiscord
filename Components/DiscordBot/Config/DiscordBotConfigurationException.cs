using System;

namespace DiscordBot.Config
{
    public class DiscordBotConfigurationException : Exception
    {
        public DiscordBotConfigurationException() { }
        public DiscordBotConfigurationException(string message) : base(message) { }
        public DiscordBotConfigurationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
