using Discord;
using Discord.WebSocket;

namespace DiscordBot.DiscordBot
{
    internal static class DiscordBotConfig
    {
        public static IDiscordBotClient CreateDiscordBot()
        {
            return new DiscordSocketClientWrapper(
                    new DiscordSocketConfig
                    {
                        AlwaysDownloadUsers = true,
                        DefaultRetryMode = RetryMode.RetryRatelimit,
                        GuildSubscriptions = true,
                        LogLevel = LogSeverity.Debug,
                        MessageCacheSize = 1000             // Cache 1,000 messages per channel
                    });
        }
    }
}
