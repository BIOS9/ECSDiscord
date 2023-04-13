using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ECSDiscord.Services;
using ECSDiscord.Util;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static ECSDiscord.Services.VerificationService;

namespace ECSDiscord.Modules
{
    [Name("Verification")]
    public class VerificationModule : ModuleBase<SocketCommandContext>
    {
        private readonly IConfiguration _config;
        private VerificationService _verification;
        private readonly StorageService _storage;

        public VerificationModule(VerificationService verification, IConfiguration config, StorageService storage)
        {
            _verification = verification;
            _config = config;
            _storage = storage;
        }

        [Command("verify")]
        [Summary("Connect your uni username with your ECS discord account.")]
        [Remarks("Supply your uni email address to verify.")]
        public async Task VerifyAsync()
        {
            if (!Context.IsPrivate)
            {
                try
                {
                    await Context.User.SendMessageAsync($"Please provide your uni student email address e.g.\n" +
                    $"```{_config["prefix"]}verify username@myvuw.ac.nz```");
                    await ReplyAsync($"I've sent you a DM with further instructions on how to verify.");
                } 
                catch (Discord.Net.HttpException)
                {
                    await ReplyAsync($":warning: Your privacy settings have prevented me from sending you a DM.\n" +
                        $"You can **Allow direct messages from server members.** under the **Privacy settings** in the server drop down on desktop or in the server menu on mobile.");
                }   
            }
            else
            {
                await ReplyAsync($"Please provide your uni student email address to verify e.g.\n" +
                    $"```{_config["prefix"]}verify username@myvuw.ac.nz```");
            }
        }


        [Command("verify")]
        [Summary("Connect your uni username with your ECS discord account.")]
        [Remarks("Supply your uni email address to verify.")]
        public async Task VerifyAsync(string email)
        {
            await ReplyAsync("Processing...");
            if(Context.Guild != null)
                await Context.Guild.DownloadUsersAsync();
            try
            {
                await Context.Message.DeleteAsync();
            }
            catch { }

            if (!Context.IsPrivate)
            {
                await ReplyAsync($":warning:  You must send me a **Direct Message** to verify (don't use this channel)\n*Right-click/tap on my profile picture and select __message__*");
                return;
            }

            StringBuilder stringBuilder = new StringBuilder();

            if (IsValidCode(email))
            {
                VerificationResult result = await _verification.FinishVerificationAsync(email, Context.User);
                switch (result)
                {
                    case VerificationResult.InvalidToken:
                        stringBuilder.Append($":warning:  Invalid verification code.\n");
                        break;
                    default:
                    case VerificationResult.Failure:
                        stringBuilder.Append($":fire:  A server error occured. Please ask an admin to check the logs.\n");
                        break;
                    case VerificationResult.Success:
                        stringBuilder.Append($":white_check_mark:  You are now verified!\n");
                        break;
                    case VerificationResult.TokenExpired:
                        stringBuilder.Append($":clock1:  That token has expired! Please verify again.\n");
                        break;
                    case VerificationResult.NotInServer:
                        stringBuilder.Append($":warning:  You are not in the Discord server!\n");
                        break;
                }   
            }
            else
            {
                EmailResult result = await _verification.StartVerificationAsync(email, Context.User);
                switch (result)
                {
                    case EmailResult.InvalidEmail:
                        stringBuilder.Append($":warning:  Invalid email address. Please use a uni student email address.\n");
                        break;
                    default:
                    case EmailResult.Failure:
                        stringBuilder.Append($":fire:  A server error occured. Please ask an admin to check the logs.\n");
                        break;
                    case EmailResult.Success:
                        stringBuilder.Append($":white_check_mark:  Verification email sent!\nPlease check your email for further instructions.\n\nIf you do not receive the email **PLEASE CHECK YOUR SPAM**");
                        break;
                }
            }
            await ReplyAsync(stringBuilder.ToString().Trim().SanitizeMentions());
        }

        [Command("ForceVerifyRole")]
        [Summary("Adds a verification override for a role.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ForceVerifyRoleAsync(SocketRole role)
        {
            await ReplyAsync("Processing...");
            if (Context.Guild != null)
                await Context.Guild.DownloadUsersAsync();
            await _verification.AddRoleVerificationOverride(role);
            await ReplyAsync(":white_check_mark:  Role verification override added.");
        }

        [Command("ForceVerifyUser")]
        [Summary("Adds a verification override for a user.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ForceVerifyUserAsync(SocketUser user)
        {
            await ReplyAsync("Processing...");
            if (Context.Guild != null)
                await Context.Guild.DownloadUsersAsync();
            await _verification.AddUserVerificationOverride(user);
            await ReplyAsync(":white_check_mark:  User verification override added.");
        }

        [Command("ForceUnverifyRole")]
        [Summary("Removes a verification override for a role.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ForceUnverifyRoleAsync(SocketRole role)
        {
            await ReplyAsync("Processing...");
            await Context.Guild?.DownloadUsersAsync();
            if (await _verification.RemoveRoleVerificationOverrideAsync(role))
            {
                await ReplyAsync(":white_check_mark:  Role verification override removed.");
            }
            else
            {
                await ReplyAsync(":x:  Role verification override not found.");
            }
        }

        [Command("ForceUnverifyUser")]
        [Summary("Removes a verification override for a user.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ForceUnverifyUserAsync(SocketUser user)
        {
            await ReplyAsync("Processing...");
            await Context.Guild?.DownloadUsersAsync();
            if (await _verification.RemoveUserVerificationOverrideAsync(user))
            {
                await ReplyAsync(":white_check_mark:  User verification override removed.");
            }
            else
            {
                await ReplyAsync(":x:  User verification override not found.");
            }
        }

        [Command("ListForcedRoleVerifications")]
        [Summary("Lists all role verification overrides.")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ListForcedRoleVerificationsAsync()
        {
            List<ulong> overrides = await _storage.Verification.GetAllVerificationOverrides(StorageService.VerificationStorage.OverrideType.ROLE);
            if (overrides.Count == 0)
            {
                await ReplyAsync("There are no role verification overrides.");
                return;
            }
            StringBuilder sb = new StringBuilder("**Role Verification Overrides:**\n```");
            overrides.ForEach(x =>
            {
                SocketRole role = Context.Guild.GetRole(x);
                sb.Append(x);
                sb.Append(" - ");
                sb.Append(role == null ? "Unknown" : role.Name);
                sb.Append("\n");
            });
            await ReplyAsync(sb.ToString().Trim().SanitizeMentions() + "```");
        }

        [Command("ListForcedUserVerifications")]
        [Summary("Lists all role verification overrides.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ListForcedUserVerificationsAsync()
        {
            List<ulong> overrides = await _storage.Verification.GetAllVerificationOverrides(StorageService.VerificationStorage.OverrideType.USER);
            if (overrides.Count == 0)
            {
                await ReplyAsync("There are no user verification overrides.");
                return;
            }
            StringBuilder sb = new StringBuilder("**User Verification Overrides:**\n```");
            overrides.ForEach(x =>
            {
                SocketUser user = Context.Guild.GetUser(x);
                sb.Append(x);
                sb.Append(" - ");
                sb.Append(user == null ? "Unknown" : $"{user.Username}#{user.Discriminator}");
                sb.Append("\n");
            });
            await ReplyAsync(sb.ToString().Trim().SanitizeMentions() + "```");
        }

        [Command("ListForcedVerifications")]
        [Summary("Lists all verification overrides.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ListForcedVerificationsAsync()
        {
            List<ulong> userOverrides = await _storage.Verification.GetAllVerificationOverrides(StorageService.VerificationStorage.OverrideType.USER);
            List<ulong> roleOverrides = await _storage.Verification.GetAllVerificationOverrides(StorageService.VerificationStorage.OverrideType.ROLE);

            StringBuilder sb = new StringBuilder("**User Verification Overrides:**\n");
            if (userOverrides.Count == 0)
                sb.Append("There are no user verification overrides.\n\n");
            else
            {
                sb.Append("```");
                userOverrides.ForEach(x =>
                {
                    sb.Append("\n");
                    SocketUser user = Context.Guild.GetUser(x);
                    sb.Append(x);
                    sb.Append(" - ");
                    sb.Append(user == null ? "Unknown" : $"{user.Username}#{user.Discriminator}");
                });
                sb.Append("```\n\n");
            }

            sb.Append("**Role Verification Overrides:**\n");
            if (roleOverrides.Count == 0)
                sb.Append("There are no role verification overrides.");
            else
            {
                sb.Append("```");
                roleOverrides.ForEach(x =>
                {
                    sb.Append("\n");
                    SocketRole role = Context.Guild.GetRole(x);
                    sb.Append(x);
                    sb.Append(" - ");
                    sb.Append(role == null ? "Unknown" : role.Name);
                });
                sb.Append("```");
            }
            await ReplyAsync(sb.ToString().SanitizeMentions());
        }

        [Command("countverifiedusers")]
        [Alias("countverified", "verified", "numberverified", "verifiedcount", "verifiednumber")]
        [Summary("Gets number of verified users.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task CountVerifiedUsersAsync()
        {
            int verifiedUsers = await _storage.Verification.GetVerifiedUsersCount();

            if (verifiedUsers == 0)
                await ReplyAsync("There are no verified users.");
            else
                await ReplyAsync($"There are {verifiedUsers} verified users.");
        }
    }
}
