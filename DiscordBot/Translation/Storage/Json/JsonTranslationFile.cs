using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DiscordBot.Translation.Storage.Json
{
    internal class JsonTranslationFile : ITranslationFile
    {
        private readonly string _filePath;

        public JsonTranslationFile(string filePath)
        {
            _filePath = filePath;
        }

        public IDictionary<string, string> Read()
        {
            return ReadAsync().Result;
        }

        public async Task<IDictionary<string, string>> ReadAsync()
        {
            if (!File.Exists(_filePath))
                throw new FileNotFoundException($"Json translation file not found: \"{_filePath}\"");

            using (FileStream fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (StreamReader sw = new StreamReader(fs))
                return JsonSerializer.Deserialize<IDictionary<string, string>>(await sw.ReadToEndAsync());
        }

        public void Write(IDictionary<string, string> translations)
        {
            WriteAsync(translations).Wait();
        }

        public async Task WriteAsync(IDictionary<string, string> translations)
        {
            using (FileStream fs = new FileStream(_filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
            using (StreamWriter sw = new StreamWriter(fs))
                await sw.WriteAsync(JsonSerializer.Serialize(translations));
        }
    }
}
