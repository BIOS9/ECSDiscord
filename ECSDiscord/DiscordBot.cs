using Discord;
using Discord.WebSocket;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECSDiscord
{
    class DiscordBot
    {
        private DiscordSocketClient _client;
        private string _token;

        public DiscordBot(string token)
        {
            _token = token;
        }

        public async Task Start()
        {
            log("Bot starting up");
            _client = new DiscordSocketClient();
            _client.Log += log;
            _client.Ready += _client_Ready;
            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();
        }

        private Task _client_Ready()
        {
            log("Bot ready");
            return Task.CompletedTask;
        }

        private void log(string msg)
        {
            Log.Information("Discord Bot: {message}", msg);
        }

        private Task log(LogMessage msg)
        {
            log(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
