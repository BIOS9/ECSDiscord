using Microsoft.Extensions.Logging;
using Serilog;

namespace DiscordBot.Logging
{
    internal static class LoggerConfig
    {
        public static ILoggerFactory CreateLoggerFactory()
        {
            return new LoggerFactory()
                .AddSerilog(new LoggerConfiguration()
                    .Enrich.FromLogContext()
                    .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss zzz}][{Level:u3}][{SourceContext}] {Message:lj}{NewLine}{Exception}")
                    .CreateLogger());
        }
    }
}
