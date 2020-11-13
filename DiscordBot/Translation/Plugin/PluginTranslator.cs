using DiscordBot.Translation.Basic;
using DiscordBot.Translation.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Translation.Plugin
{
    internal class PluginTranslator : ITranslator
    {
        private readonly ITranslator _globalTranslator;
        private readonly ITranslator _translator;

        public PluginTranslator(
            ITranslator globalTranslator,
            ITranslator translator)
        {
            _globalTranslator = globalTranslator;
            _translator = translator;
        }

        public bool ContainsTranslation(string key)
        {
            return _globalTranslator?.ContainsTranslation(key) ?? false || _translator.ContainsTranslation(key);
        }

        public IDictionary<string, string> GetTranslations()
        {
            var translations = new Dictionary<string, string>();

            // Put global translations into map.
            foreach (var t in _globalTranslator?.GetTranslations())
                translations.Add(t.Key, t.Value);

            // Overwrite existing translations and add the rest.
            foreach (var t in _translator.GetTranslations())
            {
                if (translations.ContainsKey(t.Key))
                    translations[t.Key] = t.Value;
                else
                    translations.Add(t.Key, t.Value);
            }

            return translations;
        }

        public string T(string key, params object[] values)
        {
            return _translator.T(key, values);
        }

        public string Translate(string key, params object[] values)
        {
            if (_translator.ContainsTranslation(key))
                return _translator.Translate(key, values);
            if (_globalTranslator?.ContainsTranslation(key) ?? false)
                return _globalTranslator.Translate(key, values);
            throw new TranslationNotFoundException(key);
        }
    }
}
