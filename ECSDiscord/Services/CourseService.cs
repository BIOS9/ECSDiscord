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
            public readonly ulong DiscordId;

            public Course(string code, ulong discordId)
            {
                Code = code;
                DiscordId = discordId;
            }
        }

        private class CachedCourse
        {
            public readonly string Code;
            public readonly string Description;

            public CachedCourse(string code, string description)
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

        private Dictionary<string, CachedCourse> _cachedCourses = new Dictionary<string, CachedCourse>();

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
            if(await _storage.Courses.DoesCategoryExistAsync(arg.Id))
            {
                Log.Information("Deleting category because Discord category was deleted {categoryId}", arg.Id);
                await RemoveCourseCategoryAsync(arg.Id);
            }

            if(await _storage.Courses.DoesCourseExistAsync(arg.Id))
            {
                Log.Information("Deleting course because Discord course channel was deleted {categoryId}", arg.Id);
                await RemoveCourseAsync(arg.Id);
            }
        }

        public async Task<IList<Course>> GetCourses()
        {
            return (await _storage.Courses.GetAllCoursesAsync()).Select(x => new Course(x.Key, x.Value)).ToList();
        }

        public async Task<Course> GetCourse(string course)
        {
            return new Course(course, await _storage.Courses.GetCourseDiscordIdAsync(NormaliseCourseName(course)));
        }

        public async Task<bool> CourseExists(string course)
        {
            return await _storage.Courses.DoesCourseExistAsync(NormaliseCourseName(course));
        }

        public async Task CreateCourseCategoryAsync(SocketCategoryChannel existingCategory, Regex autoImportPattern, int autoImportPriority)
        {
            Log.Information("Creating course category for existing category {categoryId} {categoryName}", existingCategory.Id, existingCategory.Name);
            await _storage.Courses.CreateCategoryAsync(existingCategory.Id, autoImportPriority.ToString(), autoImportPriority);
        }

        public async Task CreateCourseCategoryAsync(string name, Regex autoImportPattern, int autoImportPriority)
        {
            Log.Information("Creating course category {categoryName}", name);
            RestCategoryChannel category = await _discord.GetGuild(_guildId).CreateCategoryChannelAsync(name);
            await _storage.Courses.CreateCategoryAsync(category.Id, autoImportPriority.ToString(), autoImportPriority);
        }

        public async Task RemoveCourseCategoryAsync(ulong discordId)
        {
            Log.Information("Deleting course category {id}", discordId);
            SocketCategoryChannel category = _discord.GetGuild(_guildId).GetCategoryChannel(discordId);
            if(category != null)
            {
                await category.DeleteAsync();
            }
            await _storage.Courses.DeleteCategoryAsync(discordId);
        }

        public async Task CreateCourseAsync(string name)
        {
            Log.Information("Creating course {name}", name);
            SocketGuild guild = _discord.GetGuild(_guildId);
            RestTextChannel channel = await guild.CreateTextChannelAsync(name);
            await _storage.Courses.CreateCourseAsync(NormaliseCourseName(name), channel.Id);
            await OrganiseCoursePosition(channel);
        }

        public async Task CreateCourseAsync(IGuildChannel channel)
        {
            Log.Information("Creating course {name} with existing channel {channel}", channel.Name, channel.Id);
            await _storage.Courses.CreateCourseAsync(NormaliseCourseName(channel.Name), channel.Id);
            await OrganiseCoursePosition(channel);
        }

        public async Task RemoveCourseAsync(string name)
        {
            await _storage.Courses.DeleteCourseAsync(NormaliseCourseName(name));
        }

        public async Task RemoveCourseAsync(ulong discordId)
        {
            await _storage.Courses.DeleteCourseAsync(discordId);
        }

        public async Task OrganiseCoursePosition(IGuildChannel channel)
        {
            
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
                Log.Information("Course cache download started");
                const string webListUrl = "https://service-web.wgtn.ac.nz/dotnet2/catprint.aspx?d=all";
                string[] urls = new string[]
                {
                webListUrl + "&t=u" + DateTime.Now.Year,
                webListUrl + "&t=p" + DateTime.Now.Year
                };

                Dictionary<string, CachedCourse> courses = new Dictionary<string, CachedCourse>();

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
                            if (!courses.TryAdd(courseCode, new CachedCourse(courseCode, courseDescription.Trim())))
                                Log.Debug("Duplicate course from download: {course}", courseCode);
                        }
                        else
                            Log.Warning("Invalid course code from web download: {course}", courseCode);
                    }
                }
                Log.Information("Course cache download finished");
                _cachedCourses = courses; // Atomic update of courses
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Course cache download failed: {message}. No changes made.", ex.Message);
                return false;
            }
        }

        private void loadConfig()
        {
            _guildId = ulong.Parse(_config["guildId"]);
        }
    }
}
