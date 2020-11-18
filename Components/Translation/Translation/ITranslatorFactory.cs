using System.Collections.Generic;

namespace DiscordBot.Translation
{
    public interface ITranslatorFactory
    {
        ITranslator CreateTranslator(IDictionary<string, string> defaultTranslations);
    }
}