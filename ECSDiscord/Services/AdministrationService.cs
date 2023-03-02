using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

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
            _discord.MessageReceived += DiscordOnMessageReceived;
            _discord.ReactionAdded += _discord_ReactionAdded;
        }

        private async Task _discord_ReactionAdded(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.Emote.Name.Equals("📌"))
            {
                var message = await cachedMessage.GetOrDownloadAsync();
                var emotes = await message.GetReactionUsersAsync(new Emoji("📌"), 3).FlattenAsync();
                if (emotes.Count() >= 3 && ! message.IsPinned)
                {
                    await message.PinAsync();
                }
            }
        }

        private async Task DiscordOnMessageReceived(SocketMessage message)
        {
            string categoryName = (message.Channel as SocketTextChannel)?.Category?.Name?.ToLower();
            if (categoryName == null) return;
            if (message.Author.Id == 255950165200994307 && 
                (categoryName.Equals("social") || categoryName.Equals("general")))
            {
                var emote = Emote.Parse("<:yikes:1080042859278905374>");
                await message.AddReactionAsync(emote);
            }
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
