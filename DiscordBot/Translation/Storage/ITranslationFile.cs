using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Translation.Storage
{
    internal interface ITranslationFile
    {
        Task<IDictionary<string, string>> ReadAsync();
        IDictionary<string, string> Read();
        Task WriteAsync(IDictionary<string, string> translations);
        void Write(IDictionary<string, string> translations);
    }
}
