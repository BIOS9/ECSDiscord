using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using ECSDiscord.Services.Bot;
using ECSDiscord.Services.Storage;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace ECSDiscord.Services;

public class ServerMessageService : IHostedService
{
    private readonly DiscordBot _discord;
    private readonly StorageService _storage;

    public ServerMessageService(DiscordBot discordBot, StorageService storage)
    {
        Log.Debug("Server Message service loading.");
        _discord = discordBot;
        _storage = storage;
        Log.Debug("Server Message service loaded.");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<ServerMessage>> GetAllMessagesAsync()
    {
        var storageMessages = await _storage.ServerMessages.GetServerMessagesAsync();
        return await Task.WhenAll(storageMessages.Select(x => ServerMessage.CreateFromStorageAsync(
            x,
            _discord.DiscordClient.GetGuild(_discord.GuildId),
            _discord.DiscordClient)));
    }

    public async Task<ServerMessage> CreateMessageAsync(string content, string name, IUser creator,
        SocketTextChannel channel)
    {
        if (channel == null)
            throw new Exception("Channel not found.");
        var msg = await channel.SendMessageAsync(content);
        await _storage.ServerMessages.CreateServerMessageAsync(new StorageService.ServerMessageStorage.ServerMessage(
            msg.Id,
            msg.Channel.Id,
            content,
            DateTimeOffset.Now,
            creator.Id,
            DateTimeOffset.Now,
            creator.Id,
            name));
        return await ServerMessage.CreateFromStorageAsync(
            await _storage.ServerMessages.GetServerMessageAsync(msg.Id),
            _discord.DiscordClient.GetGuild(_discord.GuildId),
            _discord.DiscordClient);
    }

    public async Task<ServerMessage> GetMessageAsync(ulong id)
    {
        if (!await _storage.ServerMessages.DoesServerMessageExistAsync(id))
            throw new Exception("Message not found.");

        return await ServerMessage.CreateFromStorageAsync(
            await _storage.ServerMessages.GetServerMessageAsync(id),
            _discord.DiscordClient.GetGuild(_discord.GuildId),
            _discord.DiscordClient);
    }

    public async Task<ServerMessage> EditMessageAsync(ulong messageID, string content, string name, IUser user)
    {
        var storageMsg = await _storage.ServerMessages.GetServerMessageAsync(messageID);
        var guild = _discord.DiscordClient.GetGuild(_discord.GuildId);
        var channel = guild.GetTextChannel(storageMsg.ChannelID);
        if (channel == null)
            throw new Exception("Channel not found.");

        var msg = (IUserMessage)await channel.GetMessageAsync(storageMsg.MessageID);
        if (msg == null)
            throw new Exception("Message has been deleted/not found.");

        await msg.ModifyAsync(m => { m.Content = content; });

        await _storage.ServerMessages.CreateServerMessageAsync(new StorageService.ServerMessageStorage.ServerMessage(
            storageMsg.MessageID,
            storageMsg.ChannelID,
            content,
            storageMsg.CreatedAt,
            storageMsg.Creator,
            DateTimeOffset.Now,
            user.Id,
            name));

        return await ServerMessage.CreateFromStorageAsync(
            await _storage.ServerMessages.GetServerMessageAsync(msg.Id),
            guild,
            _discord.DiscordClient);
    }

    public async Task DeleteMessageAsync(ulong messageID)
    {
        var storageMessage =
            await _storage.ServerMessages.GetServerMessageAsync(messageID);
        if (storageMessage == null)
            throw new KeyNotFoundException("Server Message not found.");

        try
        {
            var message = await _discord.DiscordClient.GetGuild(_discord.GuildId)
                .GetTextChannel(storageMessage.ChannelID)
                .GetMessageAsync(storageMessage.MessageID);
            await message.DeleteAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to delete server message from Discord channel." + ex.Message);
        }

        await _storage.ServerMessages.DeleteServerMessageAsync(messageID);
    }

    /// <summary>
    ///     Checks if the message is still present in Discord
    /// </summary>
    /// <param name="messageID">The ID of the message.</param>
    public async Task<bool> IsMessagePresentAsync(ulong messageID)
    {
        var storageMessage =
            await _storage.ServerMessages.GetServerMessageAsync(messageID);
        if (storageMessage == null)
            throw new KeyNotFoundException("Server Message not found.");

        var message = await _discord.DiscordClient.GetGuild(_discord.GuildId)
            .GetTextChannel(storageMessage.ChannelID)
            .GetMessageAsync(storageMessage.MessageID);
        return message != null;
    }

    public class ServerMessage
    {
        public readonly ulong ChannelID;
        public readonly string Content;
        public readonly DateTimeOffset CreatedAt;
        public readonly IUser Creator;
        public readonly DateTimeOffset EditedAt;
        public readonly IUser Editor;
        public readonly ulong ID;
        public readonly IMessage Message;
        public readonly string Name;

        public ServerMessage(ulong id, ulong channelID, IMessage message, IUser creator, DateTimeOffset createdAt,
            IUser editor, DateTimeOffset editedAt, string content, string name)
        {
            ID = id;
            ChannelID = channelID;
            Message = message; // Allowed to be null
            Creator = creator ?? throw new ArgumentNullException(nameof(creator));
            CreatedAt = createdAt;
            Editor = editor ?? throw new ArgumentNullException(nameof(editor));
            EditedAt = editedAt;
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public static async Task<ServerMessage> CreateFromStorageAsync(
            StorageService.ServerMessageStorage.ServerMessage storageMessage,
            SocketGuild guild,
            DiscordSocketClient discordClient)
        {
            var channel = guild?.GetTextChannel(storageMessage.ChannelID);
            var message = await (channel?.GetMessageAsync(storageMessage.MessageID) ?? Task.FromResult<IMessage>(null));
            IUser creator = discordClient.GetUser(storageMessage.Creator);
            IUser editor = discordClient.GetUser(storageMessage.LastEditor);
            return new ServerMessage(
                storageMessage.MessageID,
                storageMessage.ChannelID,
                message,
                creator,
                storageMessage.CreatedAt,
                editor,
                storageMessage.LastEditedAt,
                storageMessage.Content,
                storageMessage.Name);
        }
    }
}