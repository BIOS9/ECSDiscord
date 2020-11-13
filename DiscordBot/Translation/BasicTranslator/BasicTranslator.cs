using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Translation.BasicTranslator
{
    internal class BasicTranslator : ITranslator
    {
        private readonly IDictionary<string, string> _translationMap;

        internal BasicTranslator(IDictionary<string, string> translationMap)
        {
            _translationMap = translationMap;
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
