using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Discord;
using Discord.WebSocket;
using ECSDiscord.Services.Bot;
using ECSDiscord.Services.Minecraft;
using ECSDiscord.Services.Storage;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace ECSDiscord.Services;

public class MinecraftService : IHostedService
{
    private readonly DiscordBot _discord;
    private readonly StorageService _storage;
    private readonly VerificationService _verification;
    private static readonly HttpClient _httpClient = new HttpClient();
    private readonly SemaphoreSlim _verifyLock = new SemaphoreSlim(1, 1);
    private readonly MinecraftAccountUpdateSource _accountUpdateSource;
    
    public enum VerificationResult
    {
        DiscordNotVerified,
        AlreadyVerified,
        VerificationLimitReached,
        Success,
    }
    
    public MinecraftService(DiscordBot discordBot, StorageService storage, VerificationService verification, MinecraftAccountUpdateSource accountUpdateSource)
    {
        Log.Debug("Minecraft service loading.");
        _discord = discordBot;
        _storage = storage;
        _verification = verification;
        _accountUpdateSource = accountUpdateSource;
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

    public async Task<Guid?> QueryMinecraftUuidAsync(string username)
    {
        string url = "https://api.minecraftservices.com/minecraft/profile/lookup/name/" +
                     HttpUtility.UrlEncode(username);
        
        HttpResponseMessage response = await _httpClient.GetAsync(url);
        if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
            
        string responseBody = await response.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(responseBody);
            
        if (jsonDocument.RootElement.TryGetProperty("id", out JsonElement idElement))
        {
            var uuidStr = idElement.GetString();
            if (uuidStr == null)
            {
                return null;
            }
            return Guid.Parse(uuidStr);
        }

        return null;
    }
    
    public async Task<IEnumerable<MinecraftAccount>> GetAllMinecraftAccountsAsync()
    {
        var minecraftAccounts = await _storage.Minecraft.GetMinecraftAccountsAsync();
        return await Task.WhenAll(minecraftAccounts.Select(x => MinecraftAccount.CreateFromStorageAsync(
            x,
            _discord.DiscordClient)));
    }

    /**
     * Performs verification checks to ensure there is only a single internal mc account for any discord user.
     */
    public async Task<VerificationResult> VerifyMinecraftAccountAsync(Guid minecraftUuid, IUser discordUser, bool isExternal)
    {
        await _verifyLock.WaitAsync();
        try
        {
            if (!await _verification.IsUserVerifiedAsync(discordUser))
            {
                return VerificationResult.DiscordNotVerified;
            }
            
            if (await _storage.Minecraft.GetMinecraftAccountAsync(minecraftUuid) != null)
            {
                return VerificationResult.AlreadyVerified;
            }
            
            // There can be multiple external accounts linked to one user.
            if (isExternal)
            {
                await _storage.Minecraft.CreateMinecraftAccountAsync(new StorageService.MinecraftStorage.MinecraftAccount(
                    minecraftUuid,
                    discordUser.Id,
                    DateTimeOffset.Now,
                    true));
            }
            else
            {
                // Only a single internal account can be linked to a user.
                var existingMc = await _storage.Minecraft.FindMinecraftAccountAsync(discordUser.Id, false);
                if (existingMc != null)
                {
                    return VerificationResult.VerificationLimitReached;
                }
                
                await _storage.Minecraft.CreateMinecraftAccountAsync(new StorageService.MinecraftStorage.MinecraftAccount(
                    minecraftUuid,
                    discordUser.Id,
                    DateTimeOffset.Now,
                    false));
            }

            _accountUpdateSource.SignalUpdate();
            return VerificationResult.Success;
        }
        finally
        {
            _verifyLock.Release();
        }
    }

    public async Task<bool> DeleteMinecraftAccountAsync(Guid minecraftUuid)
    {
        var result = await _storage.Minecraft.DeleteMinecraftAccountAsync(minecraftUuid) == 1;
        _accountUpdateSource.SignalUpdate();
        return result;
    }

    public record MinecraftAccount(
        Guid MinecraftUuid, 
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