using Discord;
using Discord.Rest;
using Discord.WebSocket;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
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

        public class CachedCourse
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

        // Permissions
        private ulong
            _verifiedRoleId,
            _verifiedAllowPerms,
            _verifiedDenyPerms,
            _everyoneAllowPerms,
            _everyoneDenyPerms,
            _joinedAllowPerms,
            _joinedDenyPerms;

        class RolePermissionOverride
        {
            public string Name;
            public ulong 
                RoleId,
                AllowedPermissions,
                DeniedPermissions;
        }

        List<RolePermissionOverride> _rolePermissionOverrides = new List<RolePermissionOverride>();

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
            if (await _storage.Courses.DoesCategoryExistAsync(arg.Id))
            {
                Log.Information("Deleting category because Discord category was deleted {categoryId}", arg.Id);
                await RemoveCourseCategoryAsync(arg.Id);
            }

            if (await _storage.Courses.DoesCourseExistAsync(arg.Id))
            {
                Log.Information("Deleting course because Discord course channel was deleted {categoryId}", arg.Id);
                await RemoveCourseAsync(arg.Id);
            }
        }

        public IDictionary<string, CachedCourse> GetCachedCourses => ImmutableDictionary.ToImmutableDictionary(_cachedCourses);

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
            await _storage.Courses.CreateCategoryAsync(existingCategory.Id, autoImportPattern?.ToString(), autoImportPriority);
        }

        public async Task CreateCourseCategoryAsync(string name, Regex autoImportPattern, int autoImportPriority)
        {
            Log.Information("Creating course category {categoryName}", name);
            RestCategoryChannel category = await _discord.GetGuild(_guildId).CreateCategoryChannelAsync(name);
            await _storage.Courses.CreateCategoryAsync(category.Id, autoImportPattern?.ToString(), autoImportPriority);
        }

        public async Task RemoveCourseCategoryAsync(ulong discordId)
        {
            Log.Information("Deleting course category {id}", discordId);
            SocketCategoryChannel category = _discord.GetGuild(_guildId).GetCategoryChannel(discordId);
            if (category != null)
            {
                await category.DeleteAsync();
            }
            await _storage.Courses.DeleteCategoryAsync(discordId);
        }

        public async Task CreateCourseAsync(string name)
        {
            string courseName = NormaliseCourseName(name);
            Log.Information("Creating course {name}", courseName);
            SocketGuild guild = _discord.GetGuild(_guildId);
            RestTextChannel channel = await guild.CreateTextChannelAsync(courseName, (a) =>
            {
                if (_cachedCourses.ContainsKey(courseName))
                {
                    a.Topic = _cachedCourses[courseName].Description;
                }
            });
            await _storage.Courses.CreateCourseAsync(courseName, channel.Id);
            await OrganiseCoursePosition(channel);
            await ApplyChannelPermissionsAsync(channel);
        }

        public async Task CreateCourseAsync(IGuildChannel channel)
        {
            if (_discord.GetGuild(_guildId).GetCategoryChannel(channel.Id) != null)
            {
                Log.Debug("Skipping creating course using category {channelid} {channelName}", channel.Id, channel.Name);
                return;
            }

            if (_discord.GetGuild(_guildId).GetVoiceChannel(channel.Id) != null)
            {
                Log.Debug("Skipping creating course using voice channel {channelid} {channelName}", channel.Id, channel.Name);
                return;
            }

            Log.Information("Creating course {name} with existing channel {channel}", channel.Name, channel.Id);
            await _storage.Courses.CreateCourseAsync(NormaliseCourseName(channel.Name), channel.Id);
            await OrganiseCoursePosition(channel);
            await ApplyChannelPermissionsAsync(channel);
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
            Log.Debug("Organising course position for {channelid} {channelName}", channel.Id, channel.Name);

            if (_discord.GetGuild(_guildId).GetCategoryChannel(channel.Id) != null)
            {
                Log.Debug("Skipping organising category {channelid} {channelName}", channel.Id, channel.Name);
                return;
            }

            if (_discord.GetGuild(_guildId).GetVoiceChannel(channel.Id) != null)
            {
                Log.Debug("Skipping creating course using voice channel {channelid} {channelName}", channel.Id, channel.Name);
                return;
            }

            IList<StorageService.CourseStorage.Category> categories = await _storage.Courses.GetCategoriesAsync();
            categories = categories.Where(x => x.AutoImportPriority >= 0).OrderByDescending(x => x.AutoImportPriority).ToList();
            Log.Debug("Found {count} enabled auto import categories.", categories.Count);

            foreach (var category in categories)
            {
                Regex pattern = null;
                try
                {
                    if (!string.IsNullOrWhiteSpace(category.AutoImportPattern))
                        pattern = new Regex(category.AutoImportPattern, RegexOptions.IgnoreCase);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Invalid auto import RegEx for category {category}", category.DiscordId);
                    continue;
                }

                if (pattern?.IsMatch(channel.Name) ?? false)
                {
                    Log.Debug("Setting course position to category {category} for {channelid} {channelName}", category.DiscordId, channel.Id, channel.Name);
                    await channel.ModifyAsync(x =>
                    {
                        x.CategoryId = category.DiscordId;
                    });
                    break;
                }
            }
        }

        public async Task<bool> ApplyChannelPermissionsAsync(IGuildChannel channel)
        {
            Log.Debug("Applying channel permissions for {channelid} {channelName}", channel.Id, channel.Name);
            string courseName = await _storage.Courses.GetCourseNameAsync(channel.Id);
            if (string.IsNullOrWhiteSpace(courseName))
            {
                Log.Warning("Attempted to apply permissions on channel {channelId} {channelName} for unknown course.", channel.Id, channel.Name);
                return false;
            }

            SocketGuild guild = _discord.GetGuild(_guildId);

            SocketRole verifiedRole = guild.GetRole(_verifiedRoleId);

            if (verifiedRole == null)
            {
                Log.Error("Invalid verified role ID configured in settings. Role not found.");
                return false;
            }

            Log.Debug("Everyone allow perms: {allow} Deny: {deny}", _everyoneAllowPerms, _everyoneDenyPerms);
            OverwritePermissions? everyonePerms = channel.GetPermissionOverwrite(guild.EveryoneRole);
            if (!everyonePerms.HasValue ||
                everyonePerms?.AllowValue != _everyoneAllowPerms ||
                everyonePerms?.DenyValue != _everyoneDenyPerms)
            {
                Log.Debug("Setting @everyone permissions for channel {channelId} {channelName}", channel.Id, channel.Name);
                await channel.AddPermissionOverwriteAsync(guild.EveryoneRole, new OverwritePermissions(_everyoneAllowPerms, _everyoneDenyPerms));
            }

            OverwritePermissions? verifiedPerms = channel.GetPermissionOverwrite(verifiedRole);
            if (!verifiedPerms.HasValue ||
                verifiedPerms?.AllowValue != _verifiedAllowPerms ||
                verifiedPerms?.DenyValue != _verifiedDenyPerms)
            {
                Log.Debug("Setting verified role permissions for channel {channelId} {channelName}", channel.Id, channel.Name);
                await channel.AddPermissionOverwriteAsync(verifiedRole, new OverwritePermissions(_verifiedAllowPerms, _verifiedDenyPerms));
            }

            foreach(var o in _rolePermissionOverrides)
            {
                try
                {
                    SocketRole role = guild.GetRole(o.RoleId);
                    OverwritePermissions? perms = channel.GetPermissionOverwrite(role);
                    if (!perms.HasValue ||
                        perms?.AllowValue != o.AllowedPermissions ||
                        perms?.DenyValue != o.DeniedPermissions)
                    {
                        Log.Debug("Setting {role} role permissions for channel {channelId} {channelName}", o.Name, channel.Id, channel.Name);
                        await channel.AddPermissionOverwriteAsync(role, new OverwritePermissions(o.AllowedPermissions, o.DeniedPermissions));
                    }
                }
                catch(Exception ex)
                {
                    Log.Error(ex, "Error setting {role} role permission override on course channel {message}", o.Name, ex.Message);
                }
            }

            HashSet<ulong> courseMemberIds = new HashSet<ulong>(await _storage.Courses.GetCourseUsersAsync(courseName));
            HashSet<ulong> joinedMembers = new HashSet<ulong>();
            HashSet<ulong> extraMembers = new HashSet<ulong>();

            // Add missing permissions
            foreach (Overwrite overwrite in channel.PermissionOverwrites)
            {
                if (overwrite.TargetType == PermissionTarget.User)
                {
                    joinedMembers.Add(overwrite.TargetId);
                    extraMembers.Add(overwrite.TargetId);

                    try
                    {
                        if (overwrite.Permissions.AllowValue != _joinedAllowPerms || overwrite.Permissions.DenyValue != _joinedDenyPerms)
                        {
                            SocketUser user = _discord.GetUser(overwrite.TargetId);
                            if (user == null)
                                continue;
                            Log.Information("Updating permission mismatch on channel {channemName} {channelId} for {user} {userId}", channel.Name, channel.Id, user.Username, user.Id);
                            await channel.AddPermissionOverwriteAsync(user, new OverwritePermissions(_joinedAllowPerms, _joinedDenyPerms));
                            await Task.Delay(100); // Help prevent API throttling
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to update permission mismatch for channel {channelName} {channelid}", channel.Name, channel.Id);
                    }
                }
            }

            extraMembers.ExceptWith(courseMemberIds);
            courseMemberIds.ExceptWith(joinedMembers);

            Log.Debug("Found {count} new permissions for {channelid} {channelName}", courseMemberIds.Count, channel.Id, channel.Name);
            Log.Debug("Found {count} old permissions for {channelid} {channelName}", extraMembers.Count, channel.Id, channel.Name);

            foreach (ulong extraMember in extraMembers)
            {
                await Task.Delay(100); // To help reduce API throttling
                SocketUser user = _discord.GetUser(extraMember);
                if (user == null)
                    continue;
                await channel.RemovePermissionOverwriteAsync(user);
                Log.Debug("Removing permission for {user} from {channelid} {channelName}", user.Id, channel.Id, channel.Name);
            }

            foreach (ulong joinedMember in courseMemberIds)
            {
                await Task.Delay(100); // To help reduce API throttling
                SocketUser user = _discord.GetUser(joinedMember);
                if (user == null)
                    continue;
                await channel.AddPermissionOverwriteAsync(user, new OverwritePermissions(_joinedAllowPerms, _joinedDenyPerms));
                Log.Debug("Adding permission for {user} to {channelid} {channelName}", user.Id, channel.Id, channel.Name);
            }

            return true;
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

        public async Task<bool> CanAutoCreateCourseAsync(string course)
        {
            List<string> patterns = await _storage.Courses.GetAutoCreatePatternsAsync();
            foreach (string pattern in patterns)
            {
                if (Regex.IsMatch(course, pattern) && _cachedCourses.ContainsKey(course))
                    return true;
            }
            return false;
        }

        public async Task<List<string>> GetAllAutoCreateCoursesAsync()
        {
            List<string> patterns = await _storage.Courses.GetAutoCreatePatternsAsync();
            List<string> courses = new List<string>();

            foreach (string course in _cachedCourses.Keys)
                foreach (string pattern in patterns)
                {
                    if (Regex.IsMatch(course, pattern) && _cachedCourses.ContainsKey(course))
                        courses.Add(course);
                }
            return courses;
        }

        public async Task<List<string>> GetAutoCreatePatternsAsync()
        {
            return await _storage.Courses.GetAutoCreatePatternsAsync();
        }

        public async Task AddAutoCreatePatternAsync(string pattern)
        {
            await _storage.Courses.AddAutoCreatePatternAsync(pattern);
        }

        public async Task DeleteAutoCreatePatternAsync(string pattern)
        {
            await _storage.Courses.DeleteAutoCreatePatternAsync(pattern);
        }


        public async Task<List<StorageService.CourseStorage.CourseAlias>> GetAllAliasesAsync()
        {
            return await _storage.Courses.GetAliasesAsync();
        }

        public async Task AddAliasAsync(string name, string target, bool hidden)
        {
            await _storage.Courses.AddAliasAsync(name, target, hidden);
        }

        public async Task DeleteAliasAsync(string name)
        {
            await _storage.Courses.DeleteAliasAsync(name);
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
                    HtmlDocument document = new HtmlDocument();

                    using (WebClient wc = new WebClient())
                    {
                        byte[] data = await wc.DownloadDataTaskAsync(url);
                        using (MemoryStream ms = new MemoryStream(data))
                        {
                            document.Load(ms);

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
                    }

                    // HTML Agility pack being weird and not freeing memory.
                    document = null;
                    web = null;
                    GC.Collect();
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

            if (!ulong.TryParse(_config["verification:verifiedRoleId"], out _verifiedRoleId))
            {
                Log.Error("Invalid verifiedRoleId configured in verification settings.");
                throw new ArgumentException("Invalid verifiedRoleId configured in verification settings.");
            }

            try
            {
                _everyoneAllowPerms = ulong.Parse(_config["courses:defaultChannelPermissions:allowed:everyone"]);
                _everyoneDenyPerms = ulong.Parse(_config["courses:defaultChannelPermissions:denied:everyone"]);

                _joinedAllowPerms = ulong.Parse(_config["courses:defaultChannelPermissions:allowed:joined"]);
                _joinedDenyPerms = ulong.Parse(_config["courses:defaultChannelPermissions:denied:joined"]);

                _verifiedAllowPerms = ulong.Parse(_config["courses:defaultChannelPermissions:allowed:verified"]);
                _verifiedDenyPerms = ulong.Parse(_config["courses:defaultChannelPermissions:denied:verified"]);
            }
            catch (Exception ex)
            {
                Log.Error("Failed to load defaultCoursePermissions from config. Please use a valid Discord permission value. See https://discordapi.com/permissions.html");
                throw ex;
            }

            try
            {
                _rolePermissionOverrides = new List<RolePermissionOverride>();
                foreach(var o in _config.GetSection("courses:channelRolePermissionOverrides").GetChildren())
                {
                    Log.Debug("Adding course channel role permission override for {role}", o.Key);
                    _rolePermissionOverrides.Add(new RolePermissionOverride()
                    {
                        Name = o.Key,
                        RoleId = ulong.Parse(o["roleId"]),
                        AllowedPermissions = ulong.Parse(o["allowed"]),
                        DeniedPermissions = ulong.Parse(o["denied"])
                    });
                }
            }
            catch(Exception ex)
            {
                Log.Error("Failed to load role permission overrides from config. Please use a valid Discord permission value. See https://discordapi.com/permissions.html");
                throw ex;
            }
        }
    }
}
