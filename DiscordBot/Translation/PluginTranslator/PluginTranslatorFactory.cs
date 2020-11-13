using DiscordBot.Translation.BasicTranslator;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Translation.PluginTranslator
{
    internal class PluginTranslatorFactory : BasicTranslatorFactory
    {
        public new ITranslator CreateTranslator(IDictionary<string, string> defaultTranslations)
        {
            return CreateTranslator(System.Reflection.Assembly.GetCallingAssembly().GetName().FullName, defaultTranslations);
        }

        public new ITranslator CreateTranslator(string scope, IDictionary<string, string> defaultTranslations)
        {
            // check if translation file exists here, if not create it and put default translations in it
            return base.CreateTranslator(defaultTranslations);
        }
    }
}
