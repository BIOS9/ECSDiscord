using Microsoft.Extensions.Configuration;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleLogging
{
    internal class LoggingConfig
    {
        public LogEventLevel? LogLevel => _logLevel;
        private readonly LogEventLevel _logLevel;

        public LoggingConfig(IConfigurationSection configurationSection)
        {
            var c = configurationSection;
            if (!c.Exists())
                return;
            Enum.TryParse(c["LogLevel"], true, out _logLevel);
        }
    }
}
