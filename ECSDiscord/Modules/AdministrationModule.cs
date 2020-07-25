using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ECSDiscord.Services;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace ECSDiscord.Modules
{
    [Name("Administration")]
    public class AdministrationModule : ModuleBase<SocketCommandContext>
    {
        private readonly Discord.Commands.CommandService _service;
        private readonly CourseService _courses;
        private readonly VerificationService _verification;
        private readonly IConfigurationRoot _config;

        public AdministrationModule(Discord.Commands.CommandService service, IConfigurationRoot config, CourseService courses, VerificationService verification)
        {
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
    }
}
