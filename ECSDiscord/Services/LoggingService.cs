using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ECSDiscord.Services
{
    public class LoggingService
    {
        private readonly DiscordSocketClient _discord;
        private readonly Discord.Commands.CommandService _commands;

        // DiscordSocketClient and CommandService are injected automatically from the IServiceProvider
        public LoggingService(DiscordSocketClient discord, Discord.Commands.CommandService commands)
        {
            _discord = discord;
            _commands = commands;

            _discord.Log += OnLogAsync;
            _commands.Log += OnLogAsync;
        }

        private Task OnLogAsync(LogMessage msg)
        {
            Log.Information(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
