using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Storage
{
    public interface IBotStorageProvider
    {
        Task<string> GetCommandPrefixAsync(ulong guildID);
    }
}
