using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Translation.BasicTranslator
{
    internal class BasicTranslatorFactory : ITranslatorFactory
    {
        public ITranslator CreateTranslator(IDictionary<string, string> defaultTranslations)
        {
            return new BasicTranslator(defaultTranslations);
        }

        public ITranslator CreateTranslator(string scope, IDictionary<string, string> defaultTranslations)
        {
            return CreateTranslator(defaultTranslations);
        }
    }
}
