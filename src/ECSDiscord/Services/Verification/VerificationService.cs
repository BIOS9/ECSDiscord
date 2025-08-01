﻿using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using ECSDiscord.Services.Bot;
using ECSDiscord.Services.Email;
using ECSDiscord.Services.Storage;
using ECSDiscord.Services.Verification;
using ECSDiscord.Util;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using static ECSDiscord.Services.Storage.StorageService;
using static ECSDiscord.Services.Storage.StorageService.VerificationStorage;

namespace ECSDiscord.Services;

public class VerificationService : IHostedService
{
    public enum EmailResult
    {
        Success,
        InvalidEmail,
        Failure
    }

    public enum VerificationResult
    {
        Success,
        InvalidToken,
        Failure,
        NotInServer,
        TokenExpired
    }

    private const int RandomTokenLength = 5; // Length in bytes. Base32 encodes 5 bytes into 8 characters.
    public static readonly TimeSpan TokenExpiryTime = TimeSpan.FromDays(7);
    private static readonly Regex CodePattern = new("^\\$[A-Z0-9]+$");
    private readonly DiscordBot _discord;
    private readonly Regex _emailPattern;
    private readonly IMailSender _mailSender;

    private readonly VerificationOptions _options;
    private readonly X509Certificate2 _publicKeyCert;
    private readonly StorageService _storageService;


    public VerificationService(IOptions<VerificationOptions> options, DiscordBot discordBot, IMailSender mailSender,
        StorageService storageService)
    {
        Log.Debug("Verification service loading");
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _discord = discordBot ?? throw new ArgumentNullException(nameof(discordBot));
        ;
        _mailSender = mailSender ?? throw new ArgumentNullException(nameof(mailSender));
        _storageService = storageService;
        _publicKeyCert = new X509Certificate2(_options.PublicKeyPath);
        _emailPattern = new Regex(_options.EmailPattern, RegexOptions.IgnoreCase);
        Log.Debug("Verification service loaded");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _discord.DiscordClient.UserJoined += _discord_UserJoined;
        _discord.DiscordClient.GuildMemberUpdated += _discord_GuildMemberUpdated;
        _discord.DiscordClient.RoleDeleted += _discord_RoleDeleted;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _discord.DiscordClient.UserJoined -= _discord_UserJoined;
        _discord.DiscordClient.GuildMemberUpdated -= _discord_GuildMemberUpdated;
        _discord.DiscordClient.RoleDeleted -= _discord_RoleDeleted;
        return Task.CompletedTask;
    }

    private async Task _discord_RoleDeleted(SocketRole arg)
    {
        await RemoveRoleVerificationOverrideAsync(arg);
    }

    private async Task _discord_GuildMemberUpdated(Cacheable<SocketGuildUser, ulong> arg1, SocketGuildUser arg2)
    {
        try
        {
            Log.Debug("Guild member updated event called for {User}", arg2.Id);
            await ApplyUserVerificationAsync(arg2);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating verification role for updated user {User} {Message}", arg2.Id, ex.Message);
        }
    }

    private async Task _discord_UserJoined(SocketGuildUser user)
    {
        try
        {
            Log.Debug("Guild user joined event called for {User}", user.Id);
            await ApplyUserVerificationAsync(user);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error giving verified role to user who joined. {Message}", ex.Message);
        }
    }

    public async Task<EmailResult> StartVerificationAsync(string email, IUser user)
    {
        try
        {
            Log.Debug("Starting verification for user: {User} {Id}", user.Username, user.Id);
            email = email.Replace('＠', '@');

            if (!IsEmailValid(email, out var username))
            {
                Log.Information("Invalid verification email address supplied by: {User} {Id}", user.Username, user.Id);
                return EmailResult.InvalidEmail;
            }

            // Create persistent verification code
            var verificationCode = await CreateVerificationCodeAsync(user.Id, username);

            var guild = _discord.DiscordClient.GetGuild(_discord.GuildId);

            if (!await _mailSender.SendMailAsync(
                    email,
                    FillTemplate(_options.MailSubjectTemplate, email, username, verificationCode, user, guild),
                    FillTemplate(_options.MailBodyTemplate, email, username, verificationCode, user, guild),
                    _options.MailBodyIsHtml))
            {
                Log.Error("Failed to send verification email to {Email} for user {User} {Id}", email, user.Username,
                    user.Id);
                return EmailResult.Failure;
            }

            return EmailResult.Success;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to send verification email: {Message}", ex.Message);
            return EmailResult.Failure;
        }
    }

    public async Task<VerificationResult> FinishVerificationAsync(string token, SocketUser user)
    {
        try
        {
            Log.Debug("Finishing verification for {Username} {Id}", user.Username, user.Id);

            var pendingVerification = await _storageService.Verification.GetPendingVerificationAsync(token.ToUpper());
            if (user.Id == pendingVerification.DiscordId)
            {
                await _storageService.Verification.DeleteCodeAsync(pendingVerification
                    .DiscordId); // Delete all pending verification codes for current user

                if (DateTime.Now - pendingVerification.CreationTime > TokenExpiryTime)
                {
                    Log.Information("User verification success for {Username} {Id}", user.Username, user.Id);
                    return VerificationResult.TokenExpired;
                }

                await _storageService.Verification.AddHistoryAsync( // Add verification history record
                    pendingVerification.EncryptedUsername,
                    pendingVerification.DiscordId);
                await _storageService.Users.SetEncryptedUsernameAsync( // Set verified username for user
                    pendingVerification.DiscordId,
                    pendingVerification.EncryptedUsername);

                var guild = _discord.DiscordClient.GetGuild(_discord.GuildId);
                IGuildUser guildUser = guild.GetUser(user.Id);
                if (guildUser == null)
                {
                    Log.Warning("User {User} is not in server, but tried to verify", user.Id);
                    return VerificationResult.NotInServer;
                }

                await ApplyUserVerificationAsync(user);

                Log.Information("User verification success for {Username} {Ud}", user.Username, user.Id);
                return VerificationResult.Success;
            }

            Log.Information(
                "Verification failed, user IDs did not match for {Username} {Id} and pending verification {PendingId}",
                user.Username, user.Id, pendingVerification.DiscordId);
            return VerificationResult.InvalidToken;
        }
        catch (RecordNotFoundException)
        {
            Log.Information("User {User} attempted verification with invalid token", user.Id);
            return VerificationResult.InvalidToken;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to verify user {User} error {Message}", user.Id, ex.Message);
            return VerificationResult.Failure;
        }
    }

    private async Task<string> CreateVerificationCodeAsync(ulong discordId, string username)
    {
        Log.Debug("Creating verification code for {Id}", discordId);

        var usernameBytes = Encoding.UTF8.GetBytes(username);
        byte[] encryptedUsername;
        using (var rsa = _publicKeyCert.GetRSAPublicKey() ??
                         throw new Exception("Missing public key from user data cert"))
        {
            encryptedUsername = rsa.Encrypt(usernameBytes, RSAEncryptionPadding.OaepSHA256);
        }

        var tokenBuffer = RandomNumberGenerator.GetBytes(RandomTokenLength);

        for (var i = 0; i < 10; ++i)
            try
            {
                var token = "$" + Base32.ToBase32String(tokenBuffer);

                await _storageService.Verification.AddPendingVerificationAsync(token, encryptedUsername, discordId);
                Log.Debug("Verification code for {Id} added on attempt {Attempt}", discordId, i);
                return token;
            }
            catch (DuplicateRecordException)
            {
                Log.Debug("Duplicate verification token encountered in storage service");
            }

        throw new Exception("Failed to create verification token, tried 10 times.");
    }

    public static bool IsValidCode(string code)
    {
        return CodePattern.IsMatch(code.ToUpper());
    }

    /// <summary>
    ///     Checks if a uni username is valid
    /// </summary>
    public bool IsEmailValid(string email, out string username)
    {
        var match = _emailPattern.Match(email);
        if (!match.Success)
        {
            username = string.Empty;
            return false;
        }

        username = match.Groups[_options.UsernamePatternGroup].Value;
        return true;
    }

    public async Task<bool> IsUserVerifiedAsync(IUser user)
    {
        return await IsUserVerifiedAsync(user.Id);
    }

    public async Task<bool> IsUserVerifiedAsync(ulong discordId)
    {
        Log.Debug("User verification check for {Id}", discordId);
        try
        {
            var guild = _discord.DiscordClient.GetGuild(_discord.GuildId);
            var guildUser = guild.GetUser(discordId);
            if (guildUser == null)
            {
                Log.Warning("User {User} not found in guild while checking verification status", discordId);
                return false;
            }

            var verificationOverrides = await _storageService.Verification.GetAllVerificationOverrides();
            if (verificationOverrides.ContainsKey(discordId) ||
                guildUser.Roles.Any(x => verificationOverrides.ContainsKey(x.Id)))
                return true;
            return await _storageService.Users.GetEncryptedUsernameAsync(discordId) != null;
        }
        catch (RecordNotFoundException)
        {
            return false;
        }
    }

    public async Task<bool> ApplyUserVerificationAsync(SocketUser user, bool allowUnverification = true)
    {
        Log.Debug("Checking user {User} verificaton status", user.Id);

        if (user.IsBot)
        {
            Log.Debug("Skipping bot user {User}", user.Id);
            return true;
        }

        var guild = _discord.DiscordClient.GetGuild(_discord.GuildId);
        var guildUser = guild.GetUser(user.Id);
        var verifiedRole = guild.GetRole(_options.VerifiedRoleId);
        var mutedRole = guild.GetRole(_options.MutedRoleId);

        if (verifiedRole == null)
        {
            Log.Error("Failed to find verified Discord role!");
            throw new Exception("Verified role not found.");
        }

        if (guildUser == null)
        {
            Log.Warning("Cannot update verification status for user. User is not in guild. {User}", user.Id);
            return false;
        }

        if (await IsUserVerifiedAsync(user))
        {
            if (mutedRole != null && guildUser.Roles.Any(x => x.Id == _options.MutedRoleId))
            {
                Log.Information("Removing verified role from muted user {User}", user.Id);
                await guildUser.RemoveRoleAsync(verifiedRole);
                Log.Debug("Successfully removed verified role from user {User}", user.Id);
                return false;
            }

            if (!guildUser.Roles.Any(x => x.Id == _options.VerifiedRoleId))
            {
                Log.Information("Giving verified role to user {User}", user.Id);
                await guildUser.AddRoleAsync(verifiedRole);
                Log.Debug("Successfully gave verified to user {User}", user.Id);
                return true;
            }

            return true;
        }

        if (allowUnverification)
        {
            if (guildUser.Roles.Any(x => x.Id == _options.VerifiedRoleId))
            {
                Log.Information("Removing verified role for user {User}", user.Id);
                await guildUser.RemoveRoleAsync(verifiedRole);
                Log.Debug("Successfully removed verified for role user {User}", user.Id);
            }

            return false;
        }

        return true; // User remains verified
    }

    public async Task ApplyRoleVerificationAsync(SocketRole role, bool allowUnverification = true)
    {
        Log.Information("Running role verification check {Role}", role.Id);
        foreach (var user in role.Members)
            try
            {
                await ApplyUserVerificationAsync(user, allowUnverification);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to apply user verification for {User} {Message}", user.Id, ex.Message);
            }
    }

    public async Task ApplyAllUserVerificationAsync(bool allowUnverification = true)
    {
        Log.Information("Running mass verification check");
        var guild = _discord.DiscordClient.GetGuild(_discord.GuildId);
        foreach (var user in guild.Users)
        {
            await Task.Delay(200);
            try
            {
                await ApplyUserVerificationAsync(user, allowUnverification);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to apply user verification for {User} {Message}", user.Id, ex.Message);
            }
        }

        Log.Information("Mass verification check finished");
    }

    public async Task AddUserVerificationOverride(SocketUser user)
    {
        var guild = _discord.DiscordClient.GetGuild(_discord.GuildId);
        if (guild.GetUser(user.Id) == null)
        {
            Log.Warning("Cannot add verification override for user. User is not in guild. {User}", user.Id);
            throw new ArgumentException("Cannot add verification override for user. User is not in guild.");
        }

        await _storageService.Verification.AddVerificationOverride(user.Id, OverrideType.USER);
        await ApplyUserVerificationAsync(user);
    }

    public async Task AddRoleVerificationOverride(SocketRole role)
    {
        var guild = _discord.DiscordClient.GetGuild(_discord.GuildId);
        if (guild.GetRole(role.Id) == null)
        {
            Log.Warning("Cannot add verification override for role. Role does not exist. {Role}", role.Id);
            throw new ArgumentException("Cannot add verification override for role. Role does not exist.");
        }

        await _storageService.Verification.AddVerificationOverride(role.Id, OverrideType.ROLE);
        await ApplyRoleVerificationAsync(role);
    }

    public async Task<bool> RemoveUserVerificationOverrideAsync(SocketUser user)
    {
        if (await _storageService.Verification.DeleteVerificationOverrideAsync(user.Id))
        {
            await ApplyUserVerificationAsync(user);
            return true;
        }

        return false;
    }

    public async Task<bool> RemoveRoleVerificationOverrideAsync(SocketRole role)
    {
        if (await _storageService.Verification.DeleteVerificationOverrideAsync(role.Id))
        {
            await ApplyRoleVerificationAsync(role);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Fills context details into a template string.
    /// </summary>
    private static string FillTemplate(string template, string email, string username, string verificationCode,
        IUser user, SocketGuild guild)
    {
        return template
            .Replace("{emailAddress}", email)
            .Replace("{username}", username)
            .Replace("{discordUsername}", user.Username)
            .Replace("{discordId}", user.Id.ToString())
            .Replace("{discordGuildName}", guild.Name)
            .Replace("{discordGuildId}", guild.Id.ToString())
            .Replace("{verificationCode}", verificationCode);
    }
}