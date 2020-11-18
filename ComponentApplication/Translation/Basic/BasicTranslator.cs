using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace DiscordBot.Translation.Basic
{
    internal class BasicTranslator : ITranslator
    {
        private readonly IReadOnlyDictionary<string, string> _translationMap;

        internal BasicTranslator(IDictionary<string, string> translationMap)
        {
            _translationMap = translationMap.ToImmutableDictionary();
        }

        public IDictionary<string, string> GetTranslations()
        {
            return (IDictionary<string, string>)_translationMap;
        }

        public bool ContainsTranslation(string key)
        {
            return _translationMap.ContainsKey(key);
        }

        public string T(string key, params object[] values)
        {
            return Translate(key, values);
        }

        public string Translate(string key, params object[] values)
        {
            if (!_translationMap.ContainsKey(key))
                throw new TranslationNotFoundException(key);
            return string.Format(_translationMap[key], values);
        }
    }
}
