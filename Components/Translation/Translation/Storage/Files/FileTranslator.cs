using DiscordBot.Translation.Basic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DiscordBot.Translation.Storage.Files
{
    internal class FileTranslator : ITranslator
    {
        private readonly ITranslator _basicTranslator;

        public FileTranslator(
            string filePath,
            IDictionary<string, string> defaultTranslations,
            ITranslationFileFactory translationFileFactory)
        {
            ITranslationFile file = translationFileFactory.CreateTranslationFile(filePath);

            if (!File.Exists(filePath) || file.Read() == null)
            {
                file.Write(defaultTranslations);
                _basicTranslator = new BasicTranslator(defaultTranslations);
                return;
            }

            IDictionary<string, string> translations = file.Read();

            // Check if there are any new translations in defaultTranslations
            bool modified = false;
            foreach (string key in defaultTranslations.Keys)
            {
                if (!translations.ContainsKey(key))
                {
                    translations.Add(key, defaultTranslations[key]);
                    modified = true;
                }
            }
            // If there are new translations, write to the file
            if (modified)
                file.Write(translations);

            _basicTranslator = new BasicTranslator(translations);
        }

        public bool ContainsTranslation(string key)
        {
            return _basicTranslator.ContainsTranslation(key);
        }

        public IDictionary<string, string> GetTranslations()
        {
            return _basicTranslator.GetTranslations();
        }

        public string T(string key, params object[] values)
        {
            return _basicTranslator.T(key, values);
        }

        public string Translate(string key, params object[] values)
        {
            return _basicTranslator.Translate(key, values);
        }
    }
}
