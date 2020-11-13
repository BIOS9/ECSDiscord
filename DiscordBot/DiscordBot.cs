using DiscordBot.Translation;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace DiscordBot
{
    public class DiscordBot : IApplication
    {
        ILogger _logger;
        ITranslator _translator;

        public DiscordBot(ILoggerFactory loggerFactory, ITranslatorFactory translatorFactory)
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
