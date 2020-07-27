using Discord;
using Discord.Commands;
using ECSDiscord.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ECSDiscord.Modules
{
    [Name("Courses")]
    public class CoursesModule : ModuleBase<SocketCommandContext>
    {
        private readonly Discord.Commands.CommandService _service;
        private readonly StorageService _storage;
        private readonly CourseService _courses;
        private readonly VerificationService _verification;
        private readonly IConfigurationRoot _config;

        public CoursesModule(Discord.Commands.CommandService service, IConfigurationRoot config, CourseService courses, VerificationService verification, StorageService storage)
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

        [Command("createcategory")]
        [Alias("addcategory")]
        [Summary("Adds a category for course channels.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task CreateCategoryAsync()
        {

        }
    }
}
