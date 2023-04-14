using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Threading;
using System.Threading.Tasks;

namespace ECSDiscord.Services
{
    public class LoggingService : IHostedService
    {
        private readonly DiscordSocketClient _discord;
        private readonly Discord.Commands.CommandService _commands;

        public LoggingService(DiscordSocketClient discord, Discord.Commands.CommandService commands)
        {
            _discord = discord;
            _commands = commands;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Log.Debug("Loading logging service.");
            _discord.Log += OnLogAsync;
            _commands.Log += OnLogAsync;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _discord.Log -= OnLogAsync;
            _commands.Log -= OnLogAsync;
            return Task.CompletedTask;
        }

        private Task OnLogAsync(LogMessage msg)
        {
            Log.Information(msg.ToString()); // Log using serilog
            return Task.CompletedTask;
        }
    }
}
