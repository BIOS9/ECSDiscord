using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Translation
{
    public class TranslationNotFoundException : Exception
    {
        public readonly string TranslationKey;
        public TranslationNotFoundException(string key)
        {
            TranslationKey = key;
        }
    }
}
