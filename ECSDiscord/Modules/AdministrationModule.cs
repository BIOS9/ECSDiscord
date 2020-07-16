using Discord;
using Discord.Commands;
using ECSDiscord.Modules.Util;
using ECSDiscord.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECSDiscord.Modules
{
    [Name("Administration")]
    public class AdministrationModule : ModuleBase<SocketCommandContext>
    {
        private readonly Discord.Commands.CommandService _service;
        private readonly CourseService _courses;
        private readonly IConfigurationRoot _config;

        public AdministrationModule(Discord.Commands.CommandService service, IConfigurationRoot config, CourseService courses)
        {
            _service = service;
            _config = config;
            _courses = courses;
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
    }
}
