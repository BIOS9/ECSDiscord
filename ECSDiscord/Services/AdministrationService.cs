using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.Hosting;
using System.Threading;
using ECSDiscord.Services.Bot;

namespace ECSDiscord.Services
{
    public class AdministrationService : IHostedService
    {
        private readonly DiscordSocketClient _discord;
        private readonly IConfiguration _config;
        private ulong _guildId;

        public AdministrationService(DiscordBot discord, IConfiguration config)
        {
            _discord = discord.DiscordClient;
            _config = config;            
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Log.Debug("Administration service loading.");
            loadConfig();
            _discord.MessageReceived += DiscordOnMessageReceived;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task DiscordOnMessageReceived(SocketMessage message)
        {
            //string categoryName = (message.Channel as SocketTextChannel)?.Category?.Name?.ToLower();
            //if (categoryName == null) return;
            //if (message.Author.Id == 255950165200994307 && 
            //    (categoryName.Equals("social") || categoryName.Equals("general")))
            //{
            //    var emote = Emote.Parse("<:yikes:1080042859278905374>");
            //    await message.AddReactionAsync(emote);
            //}
        }

        public bool IsMember(ulong discordId)
        {
            return _discord?.GetGuild(_guildId)?.GetUser(discordId) != null;
        }

        public bool IsAdmin(ulong discordId)
        {
            SocketGuild guild = _discord?.GetGuild(_guildId);
            SocketGuildUser user = guild?.GetUser(discordId);
            return user?.GuildPermissions.Administrator ?? false;
        }

        private void loadConfig()
        {
            _guildId = ulong.Parse(_config["guildId"]);
        }
    }
}
