using ComponentApplication.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;
using System.Reflection;

namespace ConsoleLogging
{
    internal class SerilogLoggerFactory : ILoggerFactory, IComponent
    {
        public string Name => "Serilog Console Logging";
        public Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        private const string ConfigSectionName = "Logging";

        private ILoggerFactory _loggerFactory;
        private LoggingConfig _config;

        public SerilogLoggerFactory(IConfigurationRoot configurationRoot)
        {
            _config = new LoggingConfig(configurationRoot.GetSection(ConfigSectionName));
            _loggerFactory = new LoggerFactory()
                   .AddSerilog(new LoggerConfiguration()
                   .Enrich.FromLogContext()
                   .MinimumLevel.Is(_config.LogLevel ?? LogEventLevel.Verbose)
                   .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss zzz}][{Level:u3}][{SourceContext}] {Message:lj}{NewLine}{Exception}")
                   .CreateLogger());
        }

        public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
        {
            return _loggerFactory.CreateLogger(categoryName);
        }

        public void AddProvider(ILoggerProvider provider)
        {
            _loggerFactory.AddProvider(provider);
        }

        public void Dispose()
        {
            _loggerFactory.Dispose();
        }
    }
}
