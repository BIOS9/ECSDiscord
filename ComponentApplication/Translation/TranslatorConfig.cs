using Autofac;
using DiscordBot.Translation.Plugin;
using DiscordBot.Translation.Storage;
using DiscordBot.Translation.Storage.Files.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Translation
{
    internal static class TranslatorConfig
    {
        public static IPluginTranslatorFactory CreatePluginTranslatorFactory(IComponentContext componentContext)
        {
            return new PluginTranslatorFactory(componentContext.Resolve<ITranslationFileFactory>()); // Use plugin translator
        }

        public static ITranslationFileFactory CreateTranslationFileFactory()
        {
            return new JsonTranslationFileFactory(); // Use Json translation files
        }
    }
}
