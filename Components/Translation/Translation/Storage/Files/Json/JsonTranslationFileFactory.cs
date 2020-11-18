using DiscordBot.Translation.Storage.Files.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Translation.Storage.Files.Json
{
    internal class JsonTranslationFileFactory : ITranslationFileFactory
    {
        public ITranslationFile CreateTranslationFile(string path)
        {
            return new JsonTranslationFile(path);
        }
    }
}
