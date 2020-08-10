using System;
using System.Collections.Generic;
using System.Text;

namespace ECSDiscord.Core.Translations
{
    public interface ITranslator
    {
        public string Translate(string key, params object[] values);
        public string T(string key, params object[] values);
    }
}
