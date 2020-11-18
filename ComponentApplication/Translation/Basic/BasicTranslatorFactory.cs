using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Translation.Basic
{
    internal class BasicTranslatorFactory : ITranslatorFactory
    {
        public virtual ITranslator CreateTranslator(IDictionary<string, string> defaultTranslations)
        {
            return new BasicTranslator(defaultTranslations);
        }
    }
}
