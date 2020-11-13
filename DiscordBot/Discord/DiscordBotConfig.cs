using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Discord
{
    internal static class DiscordBotConfig
    {
        public static DiscordSocketClient CreateDiscordBot()
        {
            return new DiscordSocketClient(new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,
                DefaultRetryMode = RetryMode.RetryRatelimit,
                GuildSubscriptions = true,
                LogLevel = LogSeverity.Verbose,
                MessageCacheSize = 1000             // Cache 1,000 messages per channel
            });
        }
    }
}
