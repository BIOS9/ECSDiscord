using System.Collections.Generic;

namespace DiscordBot.Translation.Plugin
{
    public interface IPluginTranslatorFactory : ITranslatorFactory
    {
        new ITranslator CreateTranslator(IDictionary<string, string> defaultTranslations);
        ITranslator InitializeGlobalTranslator(IDictionary<string, string> defaultTranslations);
    }
}