using Discord;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
    /// <summary>
    /// Handles log events from a Discord client and logs them to the global
    /// application logger.
    /// </summary>
    internal class BotLogger
    {
        private const string LogFormat = "<{0}> {1}"; // source, message

        private readonly ILogger _logger;

        public BotLogger(ILoggerFactory loggerFactory, string name)
        {
            _logger = loggerFactory.CreateLogger(name);
        }

        public Task Log(LogMessage arg)
        {
            string message = string.Format(LogFormat, arg.Source, arg.Message);
            switch (arg.Severity)
            {
                case LogSeverity.Critical:
                    _logger.LogCritical(arg.Exception, message);
                    break;
                case LogSeverity.Error:
                    _logger.LogError(arg.Exception, message);
                    break;
                case LogSeverity.Warning:
                    _logger.LogWarning(arg.Exception, message);
                    break;
                case LogSeverity.Info:
                    _logger.LogInformation(arg.Exception, message);
                    break;
                case LogSeverity.Verbose:
                    _logger.LogDebug(arg.Exception, message);
                    break;
                case LogSeverity.Debug:
                    _logger.LogTrace(arg.Exception, message);
                    break;
            }
            return Task.CompletedTask;
        }
    }
}
