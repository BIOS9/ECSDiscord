using Discord.WebSocket;
using DiscordBot.Translation;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace DiscordBot.Application
{
    public class DiscordBotApplication : IApplication
    {
        private readonly ILogger _logger;
        private readonly ITranslator _translator;
        private readonly DiscordSocketClient _discordClient;

        public DiscordBotApplication(
            ILoggerFactory loggerFactory,
            ITranslatorFactory translatorFactory,
            DiscordSocketClient discordClient)
        {
            _logger = loggerFactory.CreateLogger("Application");
            _translator = translatorFactory.CreateTranslator(new Dictionary<string, string>
            {
                { "STRING", "Hello!" }
            });
        }

        public void Run()
        {
            _logger.LogInformation("HI!");
            _logger.LogInformation("Test");
            _logger.LogInformation($"Translated: {_translator.T("STRING")}");
            Console.ReadLine();
        }
    }
}
