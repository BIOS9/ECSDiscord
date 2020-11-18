using ComponentApplication.Components;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace ConsoleLogging
{
    internal class SerilogLoggerFactory : ILoggerFactory, IComponent
    {
        public string Name => "Serilog Console Logging";
        public Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        private ILoggerFactory _loggerFactory;
        private Microsoft.Extensions.Logging.ILogger _logger;

        public SerilogLoggerFactory()
        {
            _loggerFactory = new LoggerFactory()
                    .AddSerilog(new LoggerConfiguration()
                    .Enrich.FromLogContext()
                    .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss zzz}][{Level:u3}][{SourceContext}] {Message:lj}{NewLine}{Exception}")
                    .CreateLogger());
            _logger = _loggerFactory.CreateLogger("Logging");
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
