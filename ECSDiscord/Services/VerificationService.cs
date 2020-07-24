using Discord.WebSocket;
using ECSDiscord.Util;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ECSDiscord.Services
{
    public class VerificationService
    {
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
            _smtpBodyTemplate;
        private int _smtpPort;
        private bool _smtpUseSsl;

        private SocketGuild _guild;
        private SocketRole _verifiedRole;

        public VerificationService(IConfigurationRoot config, DiscordSocketClient discord, StorageService storageService)
        {
            _config = config;
            _discord = discord;
            _storageService = storageService;
            loadConfig();
        }

        public enum EmailResult
        {
            Success,
            InvalidEmail,
            Failure
        }

        public async Task<EmailResult> SendVerificationEmail(string email, SocketUser user)
        {
            try
            {
                if (!IsEmailValid(email, out string username))
                    return EmailResult.InvalidEmail;

                // Create persistent verification code
                string verificationCode = await CreateVerificationCode(user.Id, username);

                SmtpClient client = new SmtpClient(_smtpHost, _smtpPort);
                client.EnableSsl = _smtpUseSsl;

                // To and from addresses
                MailAddress from = new MailAddress(_smtpFromEmail, _smtpFromName, Encoding.UTF8);
                MailAddress to = new MailAddress(email);
                
                // Create message
                MailMessage message = new MailMessage(from, to);
                message.Body = fillTemplate(_smtpBodyTemplate, email, username, verificationCode, user, _guild);
                message.BodyEncoding = Encoding.UTF8;
                message.Subject = fillTemplate(_smtpSubjectTemplate, email, username, verificationCode, user, _guild);
                message.SubjectEncoding = Encoding.UTF8;

                Log.Information("Sending verification email to: {email}", email);
                await client.SendMailAsync(message);
                Log.Debug("Verification email successfuly sent to: {email}", email);

                // Clean up
                message.Dispose();
                client.Dispose();
                
                return EmailResult.Success;
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Failed to send verification email: {message}", ex.Message);
                return EmailResult.Failure;
            }
        }

        public async Task<string> CreateVerificationCode(ulong discordId, string username)
        {
            RandomNumberGenerator rng = RNGCryptoServiceProvider.Create();

            byte[] tokenBuffer = new byte[RandomTokenLength];
            rng.GetBytes(tokenBuffer);
            string token = Base32.ToBase32String(tokenBuffer);

            await _storageService.Verification.AddVerificationCodeAsync(token, username, discordId);

            return token;
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

            _guild = _discord.GetGuild(ulong.Parse(_config["guildId"]));

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
            _smtpBodyTemplate = _config["verification:bodyTemplate"];

            if (!ulong.TryParse(_config["verification:verifiedRoleId"], out ulong roleId))
            {
                Log.Error("Invalid verifiedRoleId configured in verification settings.");
                throw new ArgumentException("Invalid verifiedRoleId configured in verification settings.");
            }

            _verifiedRole = _guild.GetRole(roleId);
        }
    }
}
