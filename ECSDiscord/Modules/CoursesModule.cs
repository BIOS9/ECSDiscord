using Discord;
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
using ECSDiscord.Util;
using ECSDiscord.Core.Translations;

namespace ECSDiscord.Modules
{
    [Name("Course Administration")]
    public class CoursesModule : ModuleBase<SocketCommandContext>
    {
        private readonly IConfigurationRoot _config;
        private readonly ITranslator _translator;
        private readonly CourseService _courseService;

        public CoursesModule(IConfigurationRoot config, ITranslator translator, CourseService courseService)
        {
            _config = config;
            _translator = translator;
            _courseService = courseService;
        }

        [Command("updatecourses")]
        [Alias("downloadcourses")]
        [Summary("Downloads the list of courses from the university website and updates the cached course list.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task UpdateCoursesAsync()
        {
            await ReplyAsync(_translator.T("COURSE_UPDATE_STARTED"));
            if (await _courseService.DownloadCourseList())
                await ReplyAsync(_translator.T("COURSE_UPDATE_SUCCESS"));
            else
                await ReplyAsync(_translator.T("COURSE_UPDATE_FAIL"));
        }

        [Command("createcategory")]
        [Alias("addcategory")]
        [Summary("Adds a category for course channels.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task CreateCategoryAsync()
        {
            string prefix = _config["prefix"];
            await ReplyAsync(_translator.T("CATEGORY_ADD_HELP", prefix));
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
            await ReplyAsync(_translator.T("COMMAND_PROCESSING"));
            Regex pattern = null;
            try
            {
                if(autoImportPattern != null && autoImportPriority != -1)
                    pattern = new Regex(autoImportPattern, RegexOptions.IgnoreCase);
            }
            catch
            {
                Log.Debug("Invalid regex supplied in createcategory command.");
                await ReplyAsync(_translator.T("INVALID_CATEGORY_AUTO_IMPORT_REGEX"));
                return;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                await ReplyAsync(_translator.T("INVALID_CATEGORY_CREATE_NAME"));
                return;
            }

            SocketCategoryChannel category = Context.Guild.CategoryChannels
                .DefaultIfEmpty(null)
                .FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (category != null)
            {
                await _courseService.CreateCourseCategoryAsync(category, pattern, autoImportPriority);
                await ReplyAsync(_translator.T("CATEGORY_ADDED_EXISTING"));
            }
            else
            {
                await _courseService.CreateCourseCategoryAsync(name, pattern, autoImportPriority);
                await ReplyAsync(_translator.T("CATEGORY_ADDED"));
            }
        }

        [Command("createcategory")]
        [Alias("addcategory")]
        [Summary("Adds a category for course channels.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task CreateCategoryAsync(SocketCategoryChannel cateogry, string autoImportPattern, int autoImportPriority)
        {
            await ReplyAsync(_translator.T("COMMAND_PROCESSING"));
            if (cateogry == null)
            {
                await ReplyAsync(_translator.T("INVALID_CATEGORY"));
                return;
            }

            Regex pattern = null;
            try
            {
                if (autoImportPattern != null && autoImportPriority != -1)
                    pattern = new Regex(autoImportPattern);
            }
            catch
            {
                Log.Debug("Invalid regex supplied in createcategory command.");
                await ReplyAsync(_translator.T("INVALID_CATEGORY_AUTO_IMPORT_REGEX"));
                return;
            }

            await _courseService.CreateCourseCategoryAsync(cateogry, pattern, autoImportPriority);
            await ReplyAsync(_translator.T("CATEGORY_ADDED_EXISTING"));
        }

        [Command("createcourse")]
        [Alias("addcourse")]
        [Summary("Adds an existing channel as a course.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task CreateCourseAsync(IGuildChannel channel)
        {
            await ReplyAsync(_translator.T("COMMAND_PROCESSING"));
            if (channel == null)
            {
                await ReplyAsync(_translator.T("INVALID_CHANNEL"));
                return;
            }

            if (await _courseService.CourseExists(channel.Name))
            {
                await ReplyAsync(_translator.T("DUPLICATE_COURSE"));
                return;
            }

            await _courseService.CreateCourseAsync(channel);
            await ReplyAsync(_translator.T("COURSE_ADDED_EXISTING_CHANNEL"));
        }

        [Command("createcourse")]
        [Alias("addcourse")]
        [Summary("Adds a new course.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task CreateCourseAsync(string courseName)
        {
            await ReplyAsync(_translator.T("COMMAND_PROCESSING"));
            if (string.IsNullOrWhiteSpace(courseName))
            {
                await ReplyAsync(_translator.T("INVALID_COURSE_CREATE_NAME"));
                return;
            }

            if (await _courseService.CourseExists(courseName))
            {
                await ReplyAsync(_translator.T("DUPLICATE_COURSE"));
                return;
            }

            await _courseService.CreateCourseAsync(courseName);
            await ReplyAsync(_translator.T("COURSE_ADDED"));
        }

        [Command("unlinkcourse")]
        [Alias("deletecourse", "removecourse")]
        [Summary("Unlinks a Discord channel from a course.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task UnlinkCourseAsync(string courseName)
        {
            await ReplyAsync(_translator.T("COMMAND_PROCESSING"));
            if (!await _courseService.CourseExists(courseName))
            {
                await ReplyAsync(_translator.T("INVALID_COURSE"));
                return;
            }

            await _courseService.RemoveCourseAsync(courseName);
            await ReplyAsync(_translator.T("COURSE_UNLINKED"));
        }

        [Command("unlinkcourse")]
        [Alias("deletecourse", "removecourse")]
        [Summary("Unlinks a Discord channel from a course.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task UnlinkCourseAsync(IGuildChannel channel)
        {
            await ReplyAsync(_translator.T("COMMAND_PROCESSING"));
            if (channel == null)
            {
                await ReplyAsync(_translator.T("INVALID_CHANNEL"));
                return;
            }

            if (!await _courseService.CourseExists(channel.Name))
            {
                await ReplyAsync(_translator.T("INVALID_COURSE"));
                return;
            }

            await _courseService.RemoveCourseAsync(channel.Name);
            await ReplyAsync(_translator.T("COURSE_UNLINKED"));
        }

        [Command("massimportcourses")]
        [Alias("massimportcourse", "importcourse", "importcourses")]
        [Summary("Imports a collection of existing course channels using a RegEx.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task MassImportCourseAsync(string regex)
        {
            await ReplyAsync(_translator.T("COMMAND_PROCESSING"));
            Regex pattern;
            try
            {
                pattern = new Regex(regex, RegexOptions.IgnoreCase);
            }
            catch
            {
                await ReplyAsync(_translator.T("INVALID_REGEX"));
                return;
            }


            List<SocketGuildChannel> channels = Context.Guild.Channels.Where(x => pattern.IsMatch(x.Name)).ToList();
            foreach (SocketGuildChannel channel in channels)
            {
                await _courseService.CreateCourseAsync(channel);
            }

            if (channels.Count > 0)
                await ReplyAsync(_translator.T("CHANNELS_IMPORTED", channels.Count));
            else
                await ReplyAsync(_translator.T("NO_CHANNELS_IMPORTED"));
        }

        [Command("massupdatepermissions")]
        [Alias("masspermissions", "massperms", "massupdateperms")]
        [Summary("Updates the permissions on course channels using a RegEx.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task MassUpdatePermissionsAsync(string regex)
        {
            await ReplyAsync(_translator.T("COMMAND_PROCESSING"));
            Regex pattern;
            try
            {
                pattern = new Regex(regex, RegexOptions.IgnoreCase);
            }
            catch
            {
                await ReplyAsync(_translator.T("INVALID_REGEX"));
                return;
            }


            List<SocketGuildChannel> channels = Context.Guild.Channels.Where(x => pattern.IsMatch(x.Name)).ToList();
            int count = 0;
            foreach (SocketGuildChannel channel in channels)
            {
                if (await _courseService.ApplyChannelPermissionsAsync(channel))
                    ++count;
            }

            await ReplyAsync(_translator.T("CHANNELS_PERMISSIONS_UPDATED", count));
        }

        [Command("updatepermissions")]
        [Alias("permissions", "perms", "updateperms")]
        [Summary("Updates the permissions on a course channel.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task UpdatePermissionsAsync(IGuildChannel channel)
        {
            await ReplyAsync(_translator.T("COMMAND_PROCESSING"));

            if (channel == null)
            {
                await ReplyAsync(_translator.T("INVALID_CHANNEL"));
                return;
            }

            await _courseService.ApplyChannelPermissionsAsync(channel);

            await ReplyAsync(_translator.T("CHANNEL_PERMISSIONS_UPDATED", MentionUtils.MentionChannel(channel.Id)));
        }

        [Command("organisechannel")]
        [Alias("organise", "layout")]
        [Summary("Moves channel based on category auto import rules.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task OrganiseChannelAsync(IGuildChannel channel)
        {
            await ReplyAsync(_translator.T("COMMAND_PROCESSING"));

            if (channel == null)
            {
                await ReplyAsync(_translator.T("INVALID_CHANNEL"));
                return;
            }

            await _courseService.OrganiseCoursePosition(channel);

            await ReplyAsync(_translator.T("CHANNEL_ORGANISED", MentionUtils.MentionChannel(channel.Id)));
        }

        [Command("massorganisechannels")]
        [Alias("massorganise", "masslayout")]
        [Summary("Moves multiple channels based on category auto import rules.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task MassOrganiseChannelAsync(string regex)
        {
            await ReplyAsync(_translator.T("COMMAND_PROCESSING"));

            Regex pattern;
            try
            {
                pattern = new Regex(regex, RegexOptions.IgnoreCase);
            }
            catch
            {
                await ReplyAsync(_translator.T("INVALID_REGEX"));
                return;
            }


            List<SocketGuildChannel> channels = Context.Guild.Channels.Where(x => pattern.IsMatch(x.Name)).ToList();
            foreach (SocketGuildChannel channel in channels)
            {
                await Task.Delay(100); // Helps prevent API throttling
                await _courseService.OrganiseCoursePosition(channel);
            }

            await ReplyAsync(_translator.T("CHANNELS_ORGANISED", channels.Count));
        }

        [Command("allcourses")]
        [Alias("listall", "listallcourses")]
        [Summary("Lists all available courses.")]
        public async Task AllCoursesAsync()
        {
            if (!Context.CheckConfigChannel("enrollments", _config)) return;

            await ReplyAsync(_translator.T("COMMAND_PROCESSING"));

            IList<CourseService.Course> courses = await _courseService.GetCourses();
            if (courses.Count == 0)
            {
                await ReplyAsync(_translator.T("NO_COURSES"));
                return;
            }

            StringBuilder builder = new StringBuilder();
            foreach (CourseService.Course course in courses)
                builder.Append(_translator.T("COURSE_LIST_ITEM", course.Code));

            await ReplyAsync(_translator.T("COURSE_LIST", builder.ToString().SanitizeMentions()));
        }
    }
}
