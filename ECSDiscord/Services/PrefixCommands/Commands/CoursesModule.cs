using Discord;
using Discord.Commands;
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

namespace ECSDiscord.Services.PrefixCommands.Commands
{
    [Name("Courses")]
    public class CoursesModule : ModuleBase<SocketCommandContext>
    {
        private readonly IConfiguration _config;
        private readonly ITranslator _translator;
        private readonly CourseService _courseService;
        private readonly EnrollmentsService _enrollments;

        public CoursesModule(IConfiguration config, ITranslator translator, CourseService courseService, EnrollmentsService enrollments)
        {
            _config = config;
            _translator = translator;
            _courseService = courseService;
            _enrollments = enrollments;
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
                if (autoImportPattern != null && autoImportPriority != -1)
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
        public async Task CreateCategoryAsync(SocketCategoryChannel category, string autoImportPattern, int autoImportPriority)
        {
            await ReplyAsync(_translator.T("COMMAND_PROCESSING"));
            if (category == null)
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

            await _courseService.CreateCourseCategoryAsync(category, pattern, autoImportPriority);
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
        [Summary("Adds one or more new courses.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task CreateCourseAsync(params string[] courseNames)
        {
            await ReplyAsync(_translator.T("COMMAND_PROCESSING"));
            foreach (string courseName in courseNames)
            {
                if (string.IsNullOrWhiteSpace(courseName))
                {
                    await ReplyAsync(_translator.T("INVALID_COURSE_CREATE_NAME", courseName));
                    continue;
                }

                if (await _courseService.CourseExists(courseName))
                {
                    await ReplyAsync(_translator.T("DUPLICATE_COURSE", courseName));
                    continue;
                }

                await _courseService.CreateCourseAsync(courseName);
                await ReplyAsync(_translator.T("COURSE_ADDED", courseName));
                await Task.Delay(200);
            }
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

            HashSet<string> courseNames = new HashSet<string>();
            courseNames.UnionWith(await _courseService.GetAllAutoCreateCoursesAsync());
            courseNames.UnionWith((await _courseService.GetCourses()).Select(x => x.Code));
            courseNames.UnionWith((await _courseService.GetAllAliasesAsync()).Where(x => !x.Hidden).Select(x => x.Name));

            if (courseNames.Count == 0)
            {
                await ReplyAsync(_translator.T("NO_COURSES"));
                return;
            }

            Regex level100Pattern = new Regex("[a-z]{4}-1[0-9]{2}", RegexOptions.IgnoreCase);
            HashSet<string> level100Courses = courseNames.Where(x => level100Pattern.IsMatch(x)).ToHashSet();

            Regex level200Pattern = new Regex("[a-z]{4}-2[0-9]{2}", RegexOptions.IgnoreCase);
            HashSet<string> level200Courses = courseNames.Where(x => level200Pattern.IsMatch(x)).ToHashSet();

            Regex level300Pattern = new Regex("[a-z]{4}-3[0-9]{2}", RegexOptions.IgnoreCase);
            HashSet<string> level300Courses = courseNames.Where(x => level300Pattern.IsMatch(x)).ToHashSet();

            Regex level400Pattern = new Regex("[a-z]{4}-4[0-9]{2}", RegexOptions.IgnoreCase);
            HashSet<string> level400Courses = courseNames.Where(x => level400Pattern.IsMatch(x)).ToHashSet();

            HashSet<string> otherCourses = new HashSet<string>(courseNames);
            otherCourses.ExceptWith(level100Courses);
            otherCourses.ExceptWith(level200Courses);
            otherCourses.ExceptWith(level300Courses);
            otherCourses.ExceptWith(level400Courses);

            string createCourseBlock(ICollection<string> courses)
            {
                List<string> courseList = courses.ToList();
                courseList.Sort();

                StringBuilder stringBuilder = new StringBuilder("```\n");
                int count = 1;
                foreach (string c in courseList)
                {
                    stringBuilder.Append(c);
                    stringBuilder.Append(count % 4 == 0 ? '\n' : '\t');
                }
                stringBuilder.Append("\n```");
                return stringBuilder.ToString();
            }

            // Credit to VicBot for this style of course listing tinyurl.com/VicBot
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle("Courses");
            builder.AddField("Usage",
                "You can manage your courses using the `+join` and `+leave` commands.\n" +
                "e.g. `+join comp102 engr101 engr121 cybr171`", false);
            if (await _enrollments.RequiresVerification(Context.User))
                builder.AddField(":rotating_light:  You are unverified!  :rotating_light:", _translator.T("ALLCOURSES_VERIFICATION_REQUIRED"), false);
            builder.AddField("100-Level", createCourseBlock(level100Courses), false);
            builder.AddField("200-Level", createCourseBlock(level200Courses), false);
            builder.AddField("300-Level", createCourseBlock(level300Courses), false);
            builder.AddField("400-Level", createCourseBlock(level400Courses), false);
            builder.AddField("Other", createCourseBlock(otherCourses), false);
            builder.WithColor(new Color(15, 84, 53));

            await ReplyAsync("", false, builder.Build());
        }

        [Command("listautocreatepatterns")]
        [Alias("autopatterns", "getautocreatepatterns")]
        [Summary("Lists all auto course create RegEx patterns.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ListAutoCreatePatternsAsync()
        {
            List<string> patterns = await _courseService.GetAutoCreatePatternsAsync();
            StringBuilder sb = new StringBuilder();
            sb.Append("```");
            foreach (string p in patterns)
            {
                sb.Append("\n");
                sb.Append(p);
            }
            sb.Append("\n```");
            await ReplyAsync(sb.ToString());
        }

        [Command("addautocreatepattern")]
        [Summary("Adds an auto course create RegEx pattern.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task AddAutoCreatePatternAsync(string pattern)
        {
            try
            {
                await _courseService.AddAutoCreatePatternAsync(pattern);
                await ReplyAsync("Added.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to add auto create pattern {message}", ex.Message);
                await ReplyAsync("Failed to add pattern due to an error.");
            }
        }

        [Command("deleteautocreatepattern")]
        [Summary("Deletes an auto course create RegEx pattern.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task DeleteAutoCreatePatternAsync(string pattern)
        {
            try
            {
                await _courseService.DeleteAutoCreatePatternAsync(pattern);
                await ReplyAsync("Deleted.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to delet  auto create pattern {message}", ex.Message);
                await ReplyAsync("Failed to delete pattern due to an error.");
            }
        }

        [Command("listcoursealiases")]
        [Alias("aliases", "listaliases")]
        [Summary("Lists all course aliases.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ListAliasesAsync()
        {
            List<StorageService.CourseStorage.CourseAlias> aliases = await _courseService.GetAllAliasesAsync();
            StringBuilder sb = new StringBuilder();
            sb.Append("```");
            foreach (var alias in aliases)
            {
                sb.Append("\n");
                sb.Append($"{alias.Name} --> {alias.Target}, Hidden: {alias.Hidden}");
            }
            sb.Append("\n```");
            await ReplyAsync(sb.ToString());
        }

        [Command("addcoursealias")]
        [Alias("addalias")]
        [Summary("Adds a course alias.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task AddAliasAsync(string name, string target, bool hidden = false)
        {
            try
            {
                await _courseService.AddAliasAsync(name, target, hidden);
                await ReplyAsync("Added.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to add alias {message}", ex.Message);
                await ReplyAsync("Failed to add alias due to an error.");
            }
        }

        [Command("deletecoursealias")]
        [Alias("deletealias")]
        [Summary("Deletes a course alias.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task DeleteAliasAsync(string name)
        {
            try
            {
                await _courseService.DeleteAliasAsync(name);
                await ReplyAsync("Deleted.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to delete alias {message}", ex.Message);
                await ReplyAsync($"Failed to delete alias due to an error: {ex.Message}");
            }
        }
    }
}
