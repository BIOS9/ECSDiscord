using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Translation
{
    internal static class TranslatorConfig
    {
        public static ITranslatorFactory CreateTranslatorFactory()
        {
            return new BasicTranslator.BasicTranslatorFactory();
        }
    }
}
