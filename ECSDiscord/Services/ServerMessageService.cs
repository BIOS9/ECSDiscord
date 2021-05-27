using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECSDiscord.Services
{
    public class ServerMessageService
    {
        private readonly DiscordSocketClient _discord;
        private readonly IConfigurationRoot _config;
        private readonly StorageService _storage;
        private ulong _guildId;

        public ServerMessageService(DiscordSocketClient discord, IConfigurationRoot config, StorageService storage)
        {
            Log.Debug("Server Message service loading.");
            _discord = discord;
            _config = config;
            _storage = storage;
            loadConfig();
            Log.Debug("Server Message service loaded.");
        }

        public class ServerMessage
        {
            public readonly IMessage Message;
            public readonly IUser Creator;
            public readonly DateTime CreatedAt;
            public readonly IUser Editor;
            public readonly DateTime EditedAt;
            public readonly string Content;

            public ServerMessage(IMessage message, IUser creator, DateTime createdAt, IUser editor, DateTime editedAt, string content)
            {
                Message = message; // Allowed to be null
                Creator = creator ?? throw new ArgumentNullException(nameof(creator));
                CreatedAt = createdAt;
                Editor = editor ?? throw new ArgumentNullException(nameof(editor));
                EditedAt = editedAt;
                Content = content ?? throw new ArgumentNullException(nameof(content));
            }

            public static async Task<ServerMessage> CreateFromStorageAsync(
                StorageService.ServerMessageStorage.ServerMessage storageMessage, 
                SocketGuild guild,
                DiscordSocketClient discordClient)
            {
                IMessage message = await guild
                    .GetTextChannel(storageMessage.ChannelID)
                    .GetMessageAsync(storageMessage.MessageID);
                IUser creator = discordClient.GetUser(storageMessage.Creator);
                IUser editor = discordClient.GetUser(storageMessage.LastEditor);
                return new ServerMessage(
                    message,
                    creator,
                    storageMessage.CreatedAt,
                    editor,
                    storageMessage.LastEditedAt,
                    storageMessage.Content);
            }
        }

        public async Task<IEnumerable<ServerMessage>> GetAllMessagesAsync()
        {
            var storageMessages = await _storage.ServerMessages.GetServerMessagesAsync();
            return await Task.WhenAll(storageMessages.Select((x) => ServerMessage.CreateFromStorageAsync(
                x,
                _discord.GetGuild(_guildId),
                _discord)));
        }

        public async Task<ServerMessage> CreateMessageAsync(string content, IUser creator, SocketTextChannel channel)
        {
            if (channel == null)
                throw new Exception("Channel not found.");
            var msg = await channel.SendMessageAsync(content);
            await _storage.ServerMessages.CreateServerMessageAsync(new StorageService.ServerMessageStorage.ServerMessage(
                msg.Id,
                msg.Channel.Id,
                content,
                DateTime.Now,
                creator.Id,
                DateTime.Now,
                creator.Id
                ));
            return await ServerMessage.CreateFromStorageAsync(
                await _storage.ServerMessages.GetServerMessageAsync(msg.Id),
                _discord.GetGuild(_guildId),
                _discord);
        }

        public async Task<ServerMessage> GetMessageAsync(ulong id)
        {
            if (!await _storage.ServerMessages.DoesServerMessageExistAsync(id))
                throw new Exception("Message not found.");

            return await ServerMessage.CreateFromStorageAsync(
                await _storage.ServerMessages.GetServerMessageAsync(id),
                _discord.GetGuild(_guildId),
                _discord);
        }

        public async Task<ServerMessage> EditMessageAsync(ulong messageID, string content, IUser user)
        {
            var storageMsg = await _storage.ServerMessages.GetServerMessageAsync(messageID);
            var guild = _discord.GetGuild(_guildId);
            var channel = guild.GetTextChannel(storageMsg.ChannelID);
            if (channel == null)
                throw new Exception("Channel not found.");
            
            var msg = (RestUserMessage)(await channel.GetMessageAsync(storageMsg.MessageID));
            if (msg == null)
                throw new Exception("Message has been deleted/not found.");

            await msg.ModifyAsync(m =>
            {
                m.Content = content;
            });
            
            await _storage.ServerMessages.CreateServerMessageAsync(new StorageService.ServerMessageStorage.ServerMessage(
                storageMsg.MessageID,
                storageMsg.ChannelID,
                content,
                storageMsg.CreatedAt,
                storageMsg.Creator,
                DateTime.Now,
                user.Id
                ));

            return await ServerMessage.CreateFromStorageAsync(
                await _storage.ServerMessages.GetServerMessageAsync(msg.Id),
                guild,
                _discord);
        }

        public async Task DeleteMessageAsync(ulong messageID)
        {
            StorageService.ServerMessageStorage.ServerMessage storageMessage = 
                await _storage.ServerMessages.GetServerMessageAsync(messageID);
            if (storageMessage == null)
                throw new KeyNotFoundException("Server Message not found.");

            IMessage message = await _discord.GetGuild(_guildId)
                .GetTextChannel(storageMessage.ChannelID)
                .GetMessageAsync(storageMessage.MessageID);
            await message.DeleteAsync();

            await _storage.ServerMessages.DeleteServerMessageAsync(messageID);
        }

        /// <summary>
        /// Checks if the message is still present in Discord
        /// </summary>
        /// <param name="messageID">The ID of the message.</param>
        public async Task<bool> IsMessagePresentAsync(ulong messageID)
        {
            StorageService.ServerMessageStorage.ServerMessage storageMessage =
                await _storage.ServerMessages.GetServerMessageAsync(messageID);
            if (storageMessage == null)
                throw new KeyNotFoundException("Server Message not found.");

            IMessage message = await _discord.GetGuild(_guildId)
                .GetTextChannel(storageMessage.ChannelID)
                .GetMessageAsync(storageMessage.MessageID);
            return message != null;
        }

        private void loadConfig()
        {
            _guildId = ulong.Parse(_config["guildId"]);
        }
    }
}
