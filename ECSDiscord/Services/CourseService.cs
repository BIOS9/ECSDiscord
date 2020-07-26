using Discord;
using Discord.Rest;
using Discord.WebSocket;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ECSDiscord.Services
{
    public class CourseService
    {
        public class Course
        {
            public readonly string Code;
            public readonly string Description;

            public Course(string code, string description)
            {
                Code = code;
                Description = description;
            }
        }

        private static readonly Regex
            CourseRegex = new Regex("([A-Za-z]+)[ \\-_]?([0-9]+)"), // Pattern for course names
            DiscordChannelRegex = new Regex("<#[0-9]{1,20}>");


        private readonly IConfigurationRoot _config;
        private readonly DiscordSocketClient _discord;
        private readonly StorageService _storage;
        private ulong _guildId;

        private Dictionary<string, Course> _courses = new Dictionary<string, Course>();

        public CourseService(IConfigurationRoot config, DiscordSocketClient discord, StorageService storage)
        {
            Log.Debug("Course service loading.");
            _config = config;
            _discord = discord;
            _storage = storage;
            _discord.ChannelDestroyed += _discord_ChannelDestroyed;
            loadConfig();
            Task.Run(DownloadCourseList);
            Log.Debug("Course service loaded.");
        }

        private async Task _discord_ChannelDestroyed(SocketChannel arg)
        {
            if(await _storage.Courses.DoesCategoryExist(arg.Id))
            {
                Log.Information("Deleting category because Discord category was deleted {categoryId}", arg.Id);
                await RemoveCourseCategory(arg.Id);
            }
        }

        public IList<Course> GetCourses()
        {
            return _courses.Values.ToList();
        }

        public Course GetCourse(string course)
        {
            return _courses[course];
        }

        public bool CourseExists(string course)
        {
            return _courses.ContainsKey(course);
        }

        public async Task<IGuildChannel> GetOrCreateChannel(string course)
        {
            if (!_courses.ContainsKey(course))
                return null;

            SocketGuild guild = _discord.GetGuild(ulong.Parse(_config["guildId"]));

            IGuildChannel channel = GetChannel(course);

            if (channel != null)
                return channel;

            RestTextChannel c = await guild.CreateTextChannelAsync(course, x =>
            {
                x.Topic = _courses[course].Description;
            });

            if (!uint.TryParse(_config["courses:courseChannelPermissionsAllowed"], out uint allowedPermissions))
            {
                Log.Error("Invalid courseChannelPermissionsAllowed permissions value in config. https://discordapi.com/permissions.html");
                return null;
            }

            if (!uint.TryParse(_config["courses:courseChannelPermissionsDenied"], out uint deniedPermissions))
            {
                Log.Error("Invalid courseChannelPermissionsDenied permissions value in config. https://discordapi.com/permissions.html");
                return null;
            }

            await c.AddPermissionOverwriteAsync(guild.EveryoneRole, new OverwritePermissions(allowedPermissions, deniedPermissions));
            return c;
        }

        public IGuildChannel GetChannel(string course)
        {
            if (!_courses.ContainsKey(course))
                return null;

            SocketGuild guild = _discord.GetGuild(ulong.Parse(_config["guildId"]));

            return guild.TextChannels
                .DefaultIfEmpty(null)
                .FirstOrDefault(x => x.Name.Equals(course, StringComparison.OrdinalIgnoreCase));
        }

        public async Task CreateCourseCategory(SocketCategoryChannel existingCategory, Regex autoImportPattern, int autoImportPriority)
        {
            Log.Information("Creating course category for existing category {categoryId} {categoryName}", existingCategory.Id, existingCategory.Name);
            await _storage.Courses.CreateCategory(existingCategory.Id, autoImportPriority.ToString(), autoImportPriority);
        }

        public async Task CreateCourseCategory(string name, Regex autoImportPattern, int autoImportPriority)
        {
            Log.Information("Creating course category {categoryName}", name);
            RestCategoryChannel category = await _discord.GetGuild(_guildId).CreateCategoryChannelAsync(name);
            await _storage.Courses.CreateCategory(category.Id, autoImportPriority.ToString(), autoImportPriority);
        }

        public async Task RemoveCourseCategory(ulong discordId)
        {
            Log.Information("Deleting course category {id}", discordId);
            SocketCategoryChannel category = _discord.GetGuild(_guildId).GetCategoryChannel(discordId);
            if(category != null)
            {
                await category.DeleteAsync();
            }
            await _storage.Courses.DeleteCategory(discordId);
        }

        /// <summary>
        /// Makes course of various different formats into the format ABCD-123
        /// </summary>
        public string NormaliseCourseName(string course)
        {
            if (DiscordChannelRegex.IsMatch(course))
            {
                try
                {
                    course = ((SocketGuildChannel)_discord.GetChannel(MentionUtils.ParseChannel(course))).Name;
                }
                catch (Exception ex)
                {
                    Log.Debug("Failed to parse discord channel name. {error}", ex.Message);
                }
            }

            Match match = CourseRegex.Match(course);
            if (!match.Success)
                return course.ToLower().Trim();

            return match.Groups[1].Value.ToUpper() + "-" + match.Groups[2].Value;
        }

        /// <summary>
        /// Downloads the course list from the VUW web site and updates the local list.
        /// </summary>
        /// <returns>Boolean indicating success.</returns>
        public async Task<bool> DownloadCourseList()
        {
            try
            {
                Log.Information("Course download started");
                const string webListUrl = "https://service-web.wgtn.ac.nz/dotnet2/catprint.aspx?d=all";
                string[] urls = new string[]
                {
                webListUrl + "&t=u" + DateTime.Now.Year,
                webListUrl + "&t=p" + DateTime.Now.Year
                };

                Dictionary<string, Course> courses = new Dictionary<string, Course>();

                foreach (string url in urls)
                {
                    Log.Debug("Downloading courses from: {url}", url);
                    HtmlWeb web = new HtmlWeb();
                    HtmlDocument document = await web.LoadFromWebAsync(url);

                    HtmlNode[] nodes = document.DocumentNode.SelectNodes("//p[@class='courseid']").ToArray();
                    foreach (HtmlNode item in nodes)
                    {
                        string courseCode = NormaliseCourseName(item.SelectSingleNode(".//span[1]").InnerText);
                        string courseDescription = item.SelectSingleNode(".//span[2]//span[1]").InnerText;

                        if (courseDescription.StartsWith("– ")) // Remove weird dash thing from start
                            courseDescription = courseDescription.Remove(0, 2);

                        if (CourseRegex.IsMatch(courseCode))
                        {
                            if (!courses.TryAdd(courseCode, new Course(courseCode, courseDescription.Trim())))
                                Log.Debug("Duplicate course from download: {course}", courseCode);
                        }
                        else
                            Log.Warning("Invalid course code from web download: {course}", courseCode);
                    }
                }
                Log.Information("Course download finished");
                _courses = courses; // Atomic update of courses
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Course download failed: {message}. No changes made.", ex.Message);
                return false;
            }
        }

        private void loadConfig()
        {
            _guildId = ulong.Parse(_config["guildId"]);
        }
    }
}
