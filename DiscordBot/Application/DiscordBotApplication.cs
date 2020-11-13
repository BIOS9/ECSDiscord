using Discord;
using Discord.WebSocket;
using DiscordBot.Translation;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordBot.Application
{
    public class DiscordBotApplication : IApplication
    {
        private readonly ILogger _logger;
        private readonly ILogger _discordBotLogger;
        private readonly ITranslator _translator;
        private readonly DiscordSocketClient _discordClient;

        public DiscordBotApplication(
            ILoggerFactory loggerFactory,
            ITranslatorFactory translatorFactory,
            DiscordSocketClient discordClient)
        {
            _logger = loggerFactory.CreateLogger("Application");
            _discordBotLogger = loggerFactory.CreateLogger("Discord");

            _translator = translatorFactory.CreateTranslator(new Dictionary<string, string>
            {
                { "STRING", "Hello!" }
            });
            _discordClient = discordClient;
            _discordClient.Log += _discordClient_Log;
        }

        private Task _discordClient_Log(LogMessage arg)
        {
            switch(arg.Severity)
            {
                case LogSeverity.Critical:
                    _discordBotLogger.LogCritical(formatDiscordLog(arg), arg.Exception);
                    break;
                case LogSeverity.Error:
                    _discordBotLogger.LogError(formatDiscordLog(arg), arg.Exception);
                    break;
                case LogSeverity.Warning:
                    _discordBotLogger.LogWarning(formatDiscordLog(arg), arg.Exception);
                    break;
                case LogSeverity.Info:
                    _discordBotLogger.LogInformation(formatDiscordLog(arg), arg.Exception);
                    break;
                case LogSeverity.Verbose:
                    _discordBotLogger.LogDebug(formatDiscordLog(arg), arg.Exception);
                    break;
                case LogSeverity.Debug:
                    _discordBotLogger.LogTrace(formatDiscordLog(arg), arg.Exception);
                    break;
            }
            return Task.CompletedTask;
        }

        private string formatDiscordLog(LogMessage arg)
        {
            return $"[{arg.Source}] {arg.Message}";
        }

        public async Task RunAsync()
        {
            _logger.LogInformation("HI!");
            _logger.LogInformation("Test");
            _logger.LogInformation($"Translated: {_translator.T("STRING")}");
            await _discordClient.StartAsync();
            Console.ReadLine();
        }
    }
}
