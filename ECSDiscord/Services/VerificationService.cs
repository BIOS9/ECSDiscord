using Discord;
using Discord.WebSocket;
using ECSDiscord.Util;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static ECSDiscord.Services.StorageService;

namespace ECSDiscord.Services
{
    public class VerificationService
    {
        public static readonly TimeSpan TokenExpiryTime = TimeSpan.FromDays(7);
        private static readonly Regex CodePattern = new Regex("^\\$[A-Z0-9]+$");

        private const int RandomTokenLength = 5; // Length in bytes. Base32 encodes 5 bytes into 8 characters.

        private readonly DiscordSocketClient _discord;
        private readonly StorageService _storageService;
        private readonly IConfigurationRoot _config;
        private Regex _emailPattern;
        private int _emailUsernameGroup;

        private string
            _smtpHost,
            _smtpFromEmail,
            _smtpFromName,
            _smtpSubjectTemplate,
            _smtpBodyTemplate,
            _publicKeyCertPath;
        private int _smtpPort;
        private bool
            _smtpUseSsl,
            _bodyIsHtml;
        private X509Certificate2 _publicKeyCert;
        private ulong _guildId;
        private ulong _verifiedRoleId;


        public VerificationService(IConfigurationRoot config, DiscordSocketClient discord, StorageService storageService)
        {
            Log.Debug("Verification service loading.");
            _config = config;
            _discord = discord;
            _storageService = storageService;
            _discord.UserJoined += _discord_UserJoined;
            loadConfig();
            Log.Debug("Verification service loaded.");
        }

        private async Task _discord_UserJoined(SocketGuildUser user)
        {
            try
            {
                Log.Debug("Checking new user {user} verificaton status.", user.Id);
                if (await IsUserVerifiedAsync(user))
                {
                    Log.Information("Giving verified role to user {user}", user.Id);
                    SocketGuild guild = _discord.GetGuild(_guildId);
                    SocketRole role = guild.GetRole(_verifiedRoleId);
                    await user.AddRoleAsync(role);
                    Log.Debug("Successfully gave verified role new user {user}.", user.Id);
                }
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Error giving verified role to user who joined. {message}", ex.Message);
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
                Log.Debug("Starting verification for user: {user} {id}", user.Username, user.Id);

                if (!IsEmailValid(email, out string username))
                {
                    Log.Information("Invalid verification email address supplied by: {user} {id}", user.Username, user.Id);
                    return EmailResult.InvalidEmail;
                }

                // Create persistent verification code
                string verificationCode = await CreateVerificationCodeAsync(user.Id, username);

                SmtpClient client = new SmtpClient(_smtpHost, _smtpPort);
                client.EnableSsl = _smtpUseSsl;

                // To and from addresses
                MailAddress from = new MailAddress(_smtpFromEmail, _smtpFromName, Encoding.UTF8);
                MailAddress to = new MailAddress(email);

                // Create message
                SocketGuild guild = _discord.GetGuild(_guildId);
                MailMessage message = new MailMessage(from, to);
                message.IsBodyHtml = _bodyIsHtml;
                message.Body = fillTemplate(_smtpBodyTemplate, email, username, verificationCode, user, guild);
                message.BodyEncoding = Encoding.UTF8;
                message.Subject = fillTemplate(_smtpSubjectTemplate, email, username, verificationCode, user, guild);
                message.SubjectEncoding = Encoding.UTF8;

                Log.Information("Sending verification email for {username} {id}", user.Username, user.Id);
                await client.SendMailAsync(message);
                Log.Debug("Verification email successfuly sent for {id}", user.Id);

                // Clean up
                message.Dispose();
                client.Dispose();

                return EmailResult.Success;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to send verification email: {message}", ex.Message);
                return EmailResult.Failure;
            }
        }

        public async Task<VerificationResult> FinishVerificationAsync(string token, SocketUser user)
        {
            try
            {
                Log.Debug("Finishing verification for {username} {id}", user.Username, user.Id);

                var pendingVerification = await _storageService.Verification.GetPendingVerificationAsync(token.ToUpper());
                if (user.Id == pendingVerification.DiscordId)
                {
                    await _storageService.Verification.DeleteCodeAsync(pendingVerification.DiscordId); // Delete all pending verification codes for current user

                    if (DateTime.Now - pendingVerification.CreationTime > TokenExpiryTime)
                    {
                        Log.Information("User verification success for {username} {id}", user.Username, user.Id);
                        return VerificationResult.TokenExpired;
                    }

                    await _storageService.Verification.AddHistoryAsync( // Add verification history record
                        pendingVerification.EncryptedUsername,
                        pendingVerification.DiscordId);
                    await _storageService.Users.SetEncryptedUsernameAsync( // Set verified username for user
                        pendingVerification.DiscordId,
                        pendingVerification.EncryptedUsername);

                    SocketGuild guild = _discord.GetGuild(_guildId);
                    SocketRole role = guild.GetRole(_verifiedRoleId);
                    if (role == null)
                    {
                        Log.Error("Failed to find verified Discord role!");
                        throw new Exception("Verified role not found.");
                    }
                    IGuildUser guildUser = guild.GetUser(user.Id);
                    if (guildUser == null)
                    {
                        Log.Warning("User {user} is not in server, but tried to verify.", user.Id);
                        return VerificationResult.NotInServer;
                    }
                    await guildUser.AddRoleAsync(role); // Give user verified role

                    Log.Information("User verification success for {username} {id}", user.Username, user.Id);
                    return VerificationResult.Success;
                }
                else
                {
                    Log.Information("Verification failed, user IDs did not match for {username} {id} and pending verification {pendingId}", user.Username, user.Id, pendingVerification.DiscordId);
                    return VerificationResult.InvalidToken;
                }
            }
            catch (StorageService.RecordNotFoundException ex)
            {
                Log.Information("User {user} attempted verification with invalid token.", user.Id);
                return VerificationResult.InvalidToken;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to verify user {user} error {message}", user.Id, ex.Message);
                return VerificationResult.Failure;
            }
        }

        private async Task<string> CreateVerificationCodeAsync(ulong discordId, string username)
        {
            Log.Debug("Creating verification code for {id}", discordId);
            RandomNumberGenerator rng = RNGCryptoServiceProvider.Create();

            byte[] usernameBytes = Encoding.UTF8.GetBytes(username);
            byte[] encryptedUsername;
            using (RSA rsa = _publicKeyCert.GetRSAPublicKey())
                encryptedUsername = rsa.Encrypt(usernameBytes, RSAEncryptionPadding.OaepSHA256);

            byte[] tokenBuffer = new byte[RandomTokenLength];

            for (int i = 0; i < 10; ++i)
            {
                try
                {
                    rng.GetBytes(tokenBuffer);
                    string token = "$" + Base32.ToBase32String(tokenBuffer);

                    await _storageService.Verification.AddPendingVerificationAsync(token, encryptedUsername, discordId);
                    Log.Debug("Verification code for {id} added on attempt {attempt}", discordId, i);
                    return token;
                }
                catch (DuplicateRecordException)
                {
                    Log.Debug("Duplicate verification token encountered in storage service.");
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
            Log.Debug("User verification check for {id}", discordId);
            try
            {
                return await _storageService.Users.GetEncryptedUsernameAsync(discordId) != null;
            }
            catch(RecordNotFoundException)
            {
                return false;
            }
        }


        /// <summary>
        /// Fills context details into a template string.
        /// </summary>
        private static string fillTemplate(string template, string email, string username, string verificationCode, SocketUser user, SocketGuild guild)
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

        private void loadConfig()
        {
            // Ensure the email regex is configured
            try
            {
                _emailPattern = new Regex(_config["verification:emailPattern"], RegexOptions.IgnoreCase);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Invalid emailPattern regex configured in verification settings.");
                throw ex;
            }

            // Ensure the username group is configured
            if (!int.TryParse(_config["verification:usernameGroup"], out _emailUsernameGroup))
            {
                Log.Error("Invalid regex usernameGroup configured in verification settings.");
                throw new ArgumentException("Invalid regex usernameGroup configured in verification settings.");
            }

            _guildId = ulong.Parse(_config["guildId"]);

            _smtpHost = _config["verification:smtpServer"];
            if (!int.TryParse(_config["verification:smtpPort"], out _smtpPort))
            {
                Log.Error("Invalid smtpPort configured in verification settings.");
                throw new ArgumentException("Invalid smtpPort configured in verification settings.");
            }

            _smtpFromEmail = _config["verification:smtpFromAddress"];
            _smtpFromName = _config["verification:smtpFromName"];

            if (!bool.TryParse(_config["verification:smtpUseSsl"], out _smtpUseSsl))
            {
                Log.Error("Invalid smtpUseSsl configured in verification settings.");
                throw new ArgumentException("Invalid smtpUseSsl configured in verification settings.");
            }

            _smtpSubjectTemplate = _config["verification:subjectTemplate"];
            if (string.IsNullOrWhiteSpace(_smtpSubjectTemplate))
            {
                Log.Error("Verification email subject cannot be empty!");
                throw new ArgumentException("Verification email subject cannot be empty!");
            }
            _smtpBodyTemplate = _config["verification:bodyTemplate"];
            if (string.IsNullOrWhiteSpace(_smtpBodyTemplate))
            {
                Log.Error("Verification email subject cannot be empty!");
                throw new ArgumentException("Verification email subject cannot be empty!");
            }

            if (!ulong.TryParse(_config["verification:verifiedRoleId"], out _verifiedRoleId))
            {
                Log.Error("Invalid verifiedRoleId configured in verification settings.");
                throw new ArgumentException("Invalid verifiedRoleId configured in verification settings.");
            }

            _publicKeyCertPath = _config["verification:publicKeyCertPath"];
            if (!File.Exists(_publicKeyCertPath))
            {
                Log.Error("Could not find public key certificate file using the path specified in verification settings.");
                throw new FileNotFoundException("Could not find public key certificate file using the path specified in verification settings.");
            }

            if (!bool.TryParse(_config["verification:bodyHtml"], out _bodyIsHtml))
            {
                Log.Error("Invalid bodyHtml boolean in verification settings.");
                throw new ArgumentException("Invalid bodyHtml boolean in verification settings.");
            }

            try
            {
                _publicKeyCert = new X509Certificate2(_publicKeyCertPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading verification certificate file {message}", ex.Message);
                throw ex;
            }
        }
    }
}
