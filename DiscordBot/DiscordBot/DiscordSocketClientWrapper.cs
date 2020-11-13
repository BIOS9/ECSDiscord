using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DiscordBot.DiscordBot
{
    internal class DiscordSocketClientWrapper : DiscordSocketClient, IDiscordBotClient
    {
        public DiscordSocketClientWrapper() { }
        public DiscordSocketClientWrapper(DiscordSocketConfig config) : base(config) { }
    }
}
