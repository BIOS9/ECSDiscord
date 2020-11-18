using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Translation
{
    public interface ITranslator
    {
        public IDictionary<string, string> GetTranslations();
        public bool ContainsTranslation(string key);
        public string Translate(string key, params object[] values);
        public string T(string key, params object[] values);
    }
}
