using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ECSDiscord.Services
{
    public class AdministrationService
    {
        private readonly DiscordSocketClient _discord;
        private readonly IConfigurationRoot _config;
        private ulong _guildId;

        public AdministrationService(DiscordSocketClient discord, IConfigurationRoot config)
        {
            Log.Debug("Administration service loading.");
            _discord = discord;
            _config = config;
            loadConfig();
            Log.Debug("Administration service loaded.");
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
