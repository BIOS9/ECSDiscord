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

public class MinecraftService : IHostedService
{
    private readonly DiscordBot _discord;
    private readonly StorageService _storage;

    public MinecraftService(DiscordBot discordBot, StorageService storage)
    {
        Log.Debug("Minecraft service loading.");
        _discord = discordBot;
        _storage = storage;
        Log.Debug("Minecraft service loaded.");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<MinecraftAccount>> GetAllMinecraftAccountsAsync()
    {
        var minecraftAccounts = await _storage.Minecraft.GetMinecraftAccountsAsync();
        return await Task.WhenAll(minecraftAccounts.Select(x => MinecraftAccount.CreateFromStorageAsync(
            x,
            _discord.DiscordClient)));
    }

    public async Task<MinecraftAccount> CreateMinecraftAccountAsync(string minecraftUuid, IUser discordUser, bool isExternal)
    {
        await _storage.Minecraft.CreateMinecraftAccountAsync(new StorageService.MinecraftStorage.MinecraftAccount(
            minecraftUuid,
            discordUser.Id,
            DateTimeOffset.Now,
            isExternal));
        return await MinecraftAccount.CreateFromStorageAsync(
            await _storage.Minecraft.GetMinecraftAccountAsync(minecraftUuid),
            _discord.DiscordClient);
    }

    public async Task DeleteMinecraftAccountAsync(string minecraftUuid)
    {
        await _storage.Minecraft.DeleteMinecraftAccountAsync(minecraftUuid);
    }

    public record MinecraftAccount(
        string MinecraftUuid, 
        IUser DiscordUser,
        DateTimeOffset CreatedAt,
        bool IsExternal)
    {
        public static async Task<MinecraftAccount> CreateFromStorageAsync(
            StorageService.MinecraftStorage.MinecraftAccount minecraftAccount,
            DiscordSocketClient discordClient)
        {
            IUser discordUser = discordClient.GetUser(minecraftAccount.DiscordId);
            return new MinecraftAccount(
                minecraftAccount.MinecraftUuid,
                discordUser,
                minecraftAccount.CreationTime,
                minecraftAccount.IsExternal);
        }
    }
}