﻿using Discord;
using Discord.Commands;
using ECSDiscord.Services;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using Discord.WebSocket;

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
            string prefix = _config["prefix"];
            await ReplyAsync($"Creates/adds a category for course channels.\n" +
                $"You can specify a RegEx auto import rule for a category to define which category new courses are added to.\n" +
                $"The auto import priority specifies the order in which the auto import rule on categories are checked. A higher value is checked before a lower value." +
                $"Use a value less than 0 disable auto import\n" +
                $"Examples:\n```{prefix}createcategory 100-Level [a-z]{{4}}-1\\d\\d 1```" +
                $"```{prefix}createcategory 733285993481896008 [a-z]{{4}}-2\\d\\d 2```" +
                $"```{prefix}createcategory \"Text Channels\"```");
        }

        [Command("createcategory")]
        [Alias("addcategory")]
        [Summary("Adds a category for course channels.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task CreateCategoryAsync(string name)
        {
            await CreateCategoryAsync(name, null, -1);
        }

        [Command("createcategory")]
        [Alias("addcategory")]
        [Summary("Adds a category for course channels.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task CreateCategoryAsync(string name, string autoImportPattern, int autoImportPriority)
        {
            Regex pattern;
            try
            {
                pattern = new Regex(autoImportPattern);
            }
            catch
            {
                Log.Debug("Invalid regex supplied in createcategory command.");
                await ReplyAsync(":warning:  Invalid auto import RegEx. Try something like `ecen-1\\d\\d` to match all 100 level ECEN courses");
                return;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                await ReplyAsync(":warning:  Invalid name.");
                return;
            }

            SocketCategoryChannel category = Context.Guild.CategoryChannels
                .DefaultIfEmpty(null)
                .FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (category != null)
            {
                await _courses.CreateCourseCategory(category, pattern, autoImportPriority);
                await ReplyAsync(":white_check_mark:  Successfully added existing category.");
            }
            else
            {
                await _courses.CreateCourseCategory(name, pattern, autoImportPriority);
                await ReplyAsync(":white_check_mark:  Successfully created new category.");
            }
        }

        [Command("createcategory")]
        [Alias("addcategory")]
        [Summary("Adds a category for course channels.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task CreateCategoryAsync(SocketCategoryChannel cateogry, string autoImportPattern, int autoImportPriority)
        {
            if(cateogry == null)
            {
                await ReplyAsync(":warning:  Invalid category.");
                return;
            }

            Regex pattern;
            try
            {
                pattern = new Regex(autoImportPattern);
            }
            catch
            {
                Log.Debug("Invalid regex supplied in createcategory command.");
                await ReplyAsync(":warning:  Invalid auto import RegEx. Try something like `ecen-1\\d\\d` to match all 100 level ECEN courses");
                return;
            }

            await _courses.CreateCourseCategory(cateogry, pattern, autoImportPriority);
            await ReplyAsync(":white_check_mark:  Successfuly added existing category.");
        }
    }
}
