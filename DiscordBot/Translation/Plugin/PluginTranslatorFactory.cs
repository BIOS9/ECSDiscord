using DiscordBot.Translation.Basic;
using DiscordBot.Translation.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DiscordBot.Translation.Plugin
{
    internal class PluginTranslatorFactory : IPluginTranslatorFactory
    {
        private const string PluginsDirectoryPath = "Plugins";
        private const string TranslationFileName = "Translations.json";
        private readonly ITranslationFileFactory _translationFileFactory;
        private ITranslator _globalTranslator;

        public PluginTranslatorFactory(ITranslationFileFactory translationFileFactory)
        {
            _translationFileFactory = translationFileFactory;
        }

        public ITranslator InitializeGlobalTranslator(IDictionary<string, string> defaultTranslations)
        {
            if (_globalTranslator != null)
                throw new AlreadyInitializedException("The global translator has already been initialized.");

            _globalTranslator = new FileTranslator(TranslationFileName, defaultTranslations, _translationFileFactory);
            return _globalTranslator;
        }

        public ITranslator CreateTranslator(IDictionary<string, string> defaultTranslations)
        {
            string assemblyName = System.Reflection.Assembly.GetCallingAssembly().GetName().Name;
            string pluginPath = Path.GetFullPath(Path.Join(PluginsDirectoryPath, assemblyName));
            if (!Directory.Exists(pluginPath))
                throw new DirectoryNotFoundException($"Plugin directory not found: \"{pluginPath}\"");
            string translationFile = Path.Join(pluginPath, TranslationFileName);

            ITranslator translator = new FileTranslator(translationFile, defaultTranslations, _translationFileFactory);
            return new PluginTranslator(_globalTranslator, translator);
        }
    }
}
