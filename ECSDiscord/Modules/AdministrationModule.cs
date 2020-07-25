using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ECSDiscord.Services;
using ECSDiscord.Util;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ECSDiscord.Modules
{
    [Name("Administration")]
    public class AdministrationModule : ModuleBase<SocketCommandContext>
    {
        private readonly Discord.Commands.CommandService _service;
        private readonly StorageService _storage;
        private readonly CourseService _courses;
        private readonly VerificationService _verification;
        private readonly IConfigurationRoot _config;

        public AdministrationModule(Discord.Commands.CommandService service, IConfigurationRoot config, CourseService courses, VerificationService verification, StorageService storage)
        {
            _storage = storage;
            _service = service;
            _config = config;
            _courses = courses;
            _verification = verification;
        }

        [Command("updatecourses")]
        [Alias("downloadcourses")]
        [Summary("Downloads the list of courses from the university website and updates the cached course list.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task UpdateCoursesAsync()
        {
            await ReplyAsync("Course update started...");
            if (await _courses.DownloadCourseList())
                await ReplyAsync("Course update succeeded.");
            else
                await ReplyAsync("Course update failed. Please check the logs for more information.");
        }

        [Command("ForceVerifyRole")]
        [Summary("Adds a verification override for a role.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ForceVerifyRoleAsync(SocketRole role)
        {
            await _verification.AddRoleVerificationOverride(role);
            await ReplyAsync(":white_check_mark:  Role verification override added.");
        }

        [Command("ForceVerifyUser")]
        [Summary("Adds a verification override for a user.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ForceVerifyUserAsync(SocketUser user)
        {
            await _verification.AddUserVerificationOverride(user);
            await ReplyAsync(":white_check_mark:  User verification override added.");
        }

        [Command("ForceUnverifyRole")]
        [Summary("Removes a verification override for a role.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ForceUnverifyRoleAsync(SocketRole role)
        {
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
            if(overrides.Count == 0)
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
    }
}
