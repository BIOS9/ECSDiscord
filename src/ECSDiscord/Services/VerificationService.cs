﻿using Discord;
using Discord.WebSocket;
using ECSDiscord.Services.Bot;
using ECSDiscord.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SendGrid;
using SendGrid.Helpers.Mail;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static ECSDiscord.Services.StorageService;
using static ECSDiscord.Services.StorageService.VerificationStorage;

namespace ECSDiscord.Services
{
    public class VerificationService : IHostedService
    {
        public static readonly TimeSpan TokenExpiryTime = TimeSpan.FromDays(7);
        private static readonly Regex CodePattern = new Regex("^\\$[A-Z0-9]+$");

        private const int RandomTokenLength = 5; // Length in bytes. Base32 encodes 5 bytes into 8 characters.

        private readonly DiscordSocketClient _discord;
        private readonly StorageService _storageService;
        private readonly IConfiguration _config;
        private Regex _emailPattern;
        private int _emailUsernameGroup;

        private string
            _sendgridFromAddress,
            _sendgridFromName,
            _sendgridSubjectTemplate,
            _sendgridBodyTemplate,
            _sendgridApiKey,

            _publicKeyCertPath;
        private bool
            _skipBots;
        private X509Certificate2 _publicKeyCert;
        private ulong _guildId;

        private ulong
            _verifiedRoleId,
            _mutedRoleId,
            _deletedMessagesChannelId;


        public VerificationService(IConfiguration config, DiscordBot discordBot, StorageService storageService)
        {
            Log.Debug("Verification service loading");
            _config = config;
            _discord = discordBot.DiscordClient;
            _storageService = storageService;
            Log.Debug("Verification service loaded");
        }

        private async Task _discord_MessageReceived(SocketMessage arg)
        {
            await ScrubDeletedMessage(arg);
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
            catch(Exception ex)
            {
                Log.Error(ex, "Error giving verified role to user who joined. {Message}", ex.Message);
            }
        }

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

        public async Task<EmailResult> StartVerificationAsync(string email, SocketUser user)
        {
            try
            {
                Log.Debug("Starting verification for user: {User} {Id}", user.Username, user.Id);
                email = email.Replace('＠', '@');

                if (!IsEmailValid(email, out string username))
                {
                    Log.Information("Invalid verification email address supplied by: {User} {Id}", user.Username, user.Id);
                    return EmailResult.InvalidEmail;
                }

                // Create persistent verification code
                string verificationCode = await CreateVerificationCodeAsync(user.Id, username);

                SocketGuild guild = _discord.GetGuild(_guildId);

                Log.Information("Sending verification email for {Username} {Id}", user.Username, user.Id);
                var client = new SendGridClient(_sendgridApiKey);
                var msg = new SendGridMessage
                {
                    From = new EmailAddress(_sendgridFromAddress, _sendgridFromName),
                    Subject = FillTemplate(_sendgridSubjectTemplate, email, username, verificationCode, user, guild),
                    HtmlContent = FillTemplate(_sendgridBodyTemplate, email, username, verificationCode, user, guild)
                };
                msg.AddTo(new EmailAddress(email));
                var response = await client.SendEmailAsync(msg);

                return response.IsSuccessStatusCode ? EmailResult.Success : EmailResult.Failure;
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
                    await _storageService.Verification.DeleteCodeAsync(pendingVerification.DiscordId); // Delete all pending verification codes for current user

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

                    SocketGuild guild = _discord.GetGuild(_guildId);
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
                else
                {
                    Log.Information("Verification failed, user IDs did not match for {Username} {Id} and pending verification {PendingId}", user.Username, user.Id, pendingVerification.DiscordId);
                    return VerificationResult.InvalidToken;
                }
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

            byte[] usernameBytes = Encoding.UTF8.GetBytes(username);
            byte[] encryptedUsername;
            using (RSA rsa = _publicKeyCert.GetRSAPublicKey() ?? throw new Exception("Missing public key from user data cert"))
                encryptedUsername = rsa.Encrypt(usernameBytes, RSAEncryptionPadding.OaepSHA256);

            byte[] tokenBuffer = RandomNumberGenerator.GetBytes(RandomTokenLength);

            for (int i = 0; i < 10; ++i)
            {
                try
                {
                    string token = "$" + Base32.ToBase32String(tokenBuffer);

                    await _storageService.Verification.AddPendingVerificationAsync(token, encryptedUsername, discordId);
                    Log.Debug("Verification code for {Id} added on attempt {Attempt}", discordId, i);
                    return token;
                }
                catch (DuplicateRecordException)
                {
                    Log.Debug("Duplicate verification token encountered in storage service");
                }
            }
            throw new Exception("Failed to create verification token, tried 10 times.");
        }

        public static bool IsValidCode(string code)
        {
            return CodePattern.IsMatch(code.ToUpper());
        }

        /// <summary>
        /// Checks if a uni username is valid
        /// </summary>
        public bool IsEmailValid(string email, out string username)
        {
            Match match = _emailPattern.Match(email);
            if (!match.Success)
            {
                username = string.Empty;
                return false;
            }

            username = match.Groups[_emailUsernameGroup].Value;
            return true;
        }

        public async Task<bool> IsUserVerifiedAsync(SocketUser user)
        {
            return await IsUserVerifiedAsync(user.Id);
        }

        public async Task<bool> IsUserVerifiedAsync(ulong discordId)
        {
            Log.Debug("User verification check for {Id}", discordId);
            try
            {
                SocketGuild guild = _discord.GetGuild(_guildId);
                SocketGuildUser guildUser = guild.GetUser(discordId);
                if(guildUser == null)
                {
                    Log.Warning("User {User} not found in guild while checking verification status", discordId);
                    return false;
                }
                Dictionary<ulong, OverrideType> verificationOverrides = await _storageService.Verification.GetAllVerificationOverrides();
                if (verificationOverrides.ContainsKey(discordId) || guildUser.Roles.Any(x => verificationOverrides.ContainsKey(x.Id)))
                    return true;
                return await _storageService.Users.GetEncryptedUsernameAsync(discordId) != null;
            }
            catch(RecordNotFoundException)
            {
                return false;
            }
        }

        public async Task<bool> ApplyUserVerificationAsync(SocketUser user, bool allowUnverification = true)
        {
            Log.Debug("Checking user {User} verificaton status", user.Id);

            if(_skipBots && user.IsBot)
            {
                Log.Debug("Skipping bot user {User}", user.Id);
                return true;
            }

            SocketGuild guild = _discord.GetGuild(_guildId);
            SocketGuildUser guildUser = guild.GetUser(user.Id);
            SocketRole verifiedRole = guild.GetRole(_verifiedRoleId);
            SocketRole mutedRole = guild.GetRole(_mutedRoleId);

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
                if (mutedRole != null && guildUser.Roles.Any(x => x.Id == _mutedRoleId))
                {
                    Log.Information("Removing verified role from muted user {User}", user.Id);
                    await guildUser.RemoveRoleAsync(verifiedRole);
                    Log.Debug("Successfully removed verified role from user {User}", user.Id);
                    return false;
                } 
                
                if (!guildUser.Roles.Any(x => x.Id == _verifiedRoleId))
                {
                    Log.Information("Giving verified role to user {User}", user.Id);
                    await guildUser.AddRoleAsync(verifiedRole);
                    Log.Debug("Successfully gave verified to user {User}", user.Id);
                    return true;
                }
                return true;
            }
            else if(allowUnverification)
            {
                if (guildUser.Roles.Any(x => x.Id == _verifiedRoleId))
                {
                    Log.Information("Removing verified role for user {User}", user.Id);
                    await guildUser.RemoveRoleAsync(verifiedRole);
                    Log.Debug("Successfully removed verified for role user {User}", user.Id);
                }

                return false;
            }
            else
            {
                return true; // User remains verified
            }
        }

        public async Task ApplyRoleVerificationAsync(SocketRole role, bool allowUnverification = true)
        {
            Log.Information("Running role verification check {Role}", role.Id);
            foreach (SocketGuildUser user in role.Members)
            {
                try
                {
                    await ApplyUserVerificationAsync(user, allowUnverification);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to apply user verification for {User} {Message}", user.Id, ex.Message);
                }
            }
        }

        public async Task ApplyAllUserVerificationAsync(bool allowUnverification = true)
        {
            Log.Information("Running mass verification check");
            SocketGuild guild = _discord.GetGuild(_guildId);
            foreach (SocketGuildUser user in guild.Users)
            {
                await Task.Delay(200);
                try
                {
                    await ApplyUserVerificationAsync(user, allowUnverification);
                }
                catch(Exception ex)
                {
                    Log.Error(ex, "Failed to apply user verification for {User} {Message}", user.Id, ex.Message);
                }
            }
            Log.Information("Mass verification check finished");
        }

        public async Task AddUserVerificationOverride(SocketUser user)
        {
            SocketGuild guild = _discord.GetGuild(_guildId);
            if(guild.GetUser(user.Id) == null)
            {
                Log.Warning("Cannot add verification override for user. User is not in guild. {User}", user.Id);
                throw new ArgumentException("Cannot add verification override for user. User is not in guild.");
            }
            await _storageService.Verification.AddVerificationOverride(user.Id, OverrideType.USER);
            await ApplyUserVerificationAsync(user);
        }

        public async Task AddRoleVerificationOverride(SocketRole role)
        {
            SocketGuild guild = _discord.GetGuild(_guildId);
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
        /// Fills context details into a template string.
        /// </summary>
        private static string FillTemplate(string template, string email, string username, string verificationCode, SocketUser user, SocketGuild guild)
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

        private async Task ScrubDeletedMessage(SocketMessage arg)
        {
            if(arg.Channel.Id == _deletedMessagesChannelId)
            {
                if (arg.Embeds.First().Description.ToLower().Contains("+verify"))
                {
                    await arg.DeleteAsync();
                    Log.Information("Scrubbed verify command from deleted messages channel");
                }
            }
        }

        private void LoadConfig()
        {
            // Ensure the email regex is configured
            try
            {
                _emailPattern = new Regex(_config["verification:emailPattern"] ?? throw new Exception("Missing email pattern"), RegexOptions.IgnoreCase);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Invalid emailPattern regex configured in verification settings");
                throw;
            }

            // Ensure the username group is configured
            if (!int.TryParse(_config["verification:usernameGroup"], out _emailUsernameGroup))
            {
                Log.Error("Invalid regex usernameGroup configured in verification settings");
                throw new ArgumentException("Invalid regex usernameGroup configured in verification settings.");
            }

            _guildId = ulong.Parse(_config["guildId"] ?? throw new Exception("Missing email pattern"));

            _sendgridFromAddress = _config["verification:fromAddress"];
            _sendgridFromName = _config["verification:fromName"];

            if (!bool.TryParse(_config["verification:skipBots"], out _skipBots))
            {
                Log.Error("Invalid skipBots configured in verification settings");
                throw new ArgumentException("Invalid skipBots configured in verification settings.");
            }

            _sendgridSubjectTemplate = _config["verification:subjectTemplate"];
            if (string.IsNullOrWhiteSpace(_sendgridSubjectTemplate))
            {
                Log.Error("Verification email subject cannot be empty!");
                throw new ArgumentException("Verification email subject cannot be empty!");
            }
            _sendgridBodyTemplate = _config["verification:bodyTemplate"];
            if (string.IsNullOrWhiteSpace(_sendgridBodyTemplate))
            {
                Log.Error("Verification email subject cannot be empty!");
                throw new ArgumentException("Verification email subject cannot be empty!");
            }

            _sendgridApiKey = _config["verification:sendgridApiKey"];
            if (string.IsNullOrWhiteSpace(_sendgridApiKey))
            {
                Log.Error("Verification sendgrid API Key cannot be empty!");
                throw new ArgumentException("Verification sendgrid API Key cannot be empty!");
            }

            if (!ulong.TryParse(_config["verification:verifiedRoleId"], out _verifiedRoleId))
            {
                Log.Error("Invalid verifiedRoleId configured in verification settings");
                throw new ArgumentException("Invalid verifiedRoleId configured in verification settings.");
            }

            if (!ulong.TryParse(_config["verification:mutedRoleId"], out _mutedRoleId))
            {
                Log.Error("Invalid mutedRoleId configured in verification settings");
                throw new ArgumentException("Invalid mutedRoleId configured in verification settings.");
            }

            if (!ulong.TryParse(_config["deletedMessagesChannelId"], out _deletedMessagesChannelId))
            {
                Log.Error("Invalid deletedMessagesChannelId configured in settings");
                throw new ArgumentException("Invalid deletedMessagesChannelId configured in settings.");
            }

            _publicKeyCertPath = _config["verification:publicKeyCertPath"];
            if (!File.Exists(_publicKeyCertPath))
            {
                Log.Error("Could not find public key certificate file using the path specified in verification settings");
                throw new FileNotFoundException("Could not find public key certificate file using the path specified in verification settings.");
            }

            try
            {
                _publicKeyCert = new X509Certificate2(_publicKeyCertPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading verification certificate file {Message}", ex.Message);
                throw;
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _discord.UserJoined += _discord_UserJoined;
            _discord.GuildMemberUpdated += _discord_GuildMemberUpdated;
            _discord.RoleDeleted += _discord_RoleDeleted;
            _discord.MessageReceived += _discord_MessageReceived;
            LoadConfig();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _discord.UserJoined -= _discord_UserJoined;
            _discord.GuildMemberUpdated -= _discord_GuildMemberUpdated;
            _discord.RoleDeleted -= _discord_RoleDeleted;
            _discord.MessageReceived -= _discord_MessageReceived;
            return Task.CompletedTask;
        }
    }
}
