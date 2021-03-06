﻿using Discord;
using Discord.WebSocket;
using Serilog;
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
            Log.Debug("Logging service loading.");
            _discord = discord;
            _commands = commands;

            _discord.Log += OnLogAsync;
            _commands.Log += OnLogAsync;
            Log.Debug("Logging service loaded (ironic).");
        }

        private Task OnLogAsync(LogMessage msg)
        {
            Log.Information(msg.ToString()); // Log using serilog
            return Task.CompletedTask;
        }
    }
}
