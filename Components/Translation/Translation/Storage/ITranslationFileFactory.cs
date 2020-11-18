using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Translation.Storage
{
    internal interface ITranslationFileFactory
    {
        ITranslationFile CreateTranslationFile(string path);
    }
}
