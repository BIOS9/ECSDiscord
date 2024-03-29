﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using ECSDiscord.Services.Bot;
using ECSDiscord.Services.Storage;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace ECSDiscord.Services;

public class CourseService : IHostedService
{
    private static readonly Regex
        CourseRegex = new("([A-Za-z]+)[ \\-_]?([0-9]+)"), // Pattern for course names
        DiscordChannelRegex = new("<#[0-9]{1,20}>");

    private readonly IConfiguration _config;
    private readonly DiscordBot _discord;
    private readonly StorageService _storage;

    private Dictionary<string, CachedCourse> _cachedCourses = new();

    private List<RolePermissionOverride> _rolePermissionOverrides = new();

    // Permissions
    private ulong
        _verifiedRoleId,
        _verifiedAllowPerms,
        _verifiedDenyPerms,
        _everyoneAllowPerms,
        _everyoneDenyPerms,
        _joinedAllowPerms,
        _joinedDenyPerms;

    public CourseService(IConfiguration config, DiscordBot discordBot, StorageService storage)
    {
        Log.Debug("Course service loading.");
        _config = config;
        _discord = discordBot;
        _storage = storage;
        Log.Debug("Course service loaded.");
    }

    public IDictionary<string, CachedCourse> GetCachedCourses => _cachedCourses.ToImmutableDictionary();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _discord.DiscordClient.ChannelDestroyed += _discord_ChannelDestroyed;
        loadConfig();
        await DownloadCourseList();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _discord.DiscordClient.ChannelDestroyed -= _discord_ChannelDestroyed;
        return Task.CompletedTask;
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

    public async Task CreateCourseCategoryAsync(SocketCategoryChannel existingCategory, Regex autoImportPattern,
        int autoImportPriority)
    {
        Log.Information("Creating course category for existing category {categoryId} {categoryName}",
            existingCategory.Id, existingCategory.Name);
        await _storage.Courses.CreateCategoryAsync(existingCategory.Id, autoImportPattern?.ToString(),
            autoImportPriority);
    }

    public async Task CreateCourseCategoryAsync(string name, Regex autoImportPattern, int autoImportPriority)
    {
        Log.Information("Creating course category {categoryName}", name);
        var category = await _discord.DiscordClient.GetGuild(_discord.GuildId).CreateCategoryChannelAsync(name);
        await _storage.Courses.CreateCategoryAsync(category.Id, autoImportPattern?.ToString(), autoImportPriority);
    }

    public async Task RemoveCourseCategoryAsync(ulong discordId)
    {
        Log.Information("Deleting course category {id}", discordId);
        var category = _discord.DiscordClient.GetGuild(_discord.GuildId).GetCategoryChannel(discordId);
        if (category != null) await category.DeleteAsync();
        await _storage.Courses.DeleteCategoryAsync(discordId);
    }

    public async Task CreateCourseAsync(string name)
    {
        var courseName = NormaliseCourseName(name);
        Log.Information("Creating course {name}", courseName);
        var guild = _discord.DiscordClient.GetGuild(_discord.GuildId);
        var channel = await guild.CreateTextChannelAsync(courseName, a =>
        {
            if (_cachedCourses.ContainsKey(courseName)) a.Topic = _cachedCourses[courseName].Description;
        });
        await _storage.Courses.CreateCourseAsync(courseName, channel.Id);
        await OrganiseCoursePosition(channel);
        await ApplyChannelPermissionsAsync(channel);
    }

    public async Task CreateCourseAsync(IGuildChannel channel)
    {
        if (_discord.DiscordClient.GetGuild(_discord.GuildId).GetCategoryChannel(channel.Id) != null)
        {
            Log.Debug("Skipping creating course using category {channelid} {channelName}", channel.Id, channel.Name);
            return;
        }

        if (_discord.DiscordClient.GetGuild(_discord.GuildId).GetVoiceChannel(channel.Id) != null)
        {
            Log.Debug("Skipping creating course using voice channel {channelid} {channelName}", channel.Id,
                channel.Name);
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

        if (_discord.DiscordClient.GetGuild(_discord.GuildId).GetCategoryChannel(channel.Id) != null)
        {
            Log.Debug("Skipping organising category {channelid} {channelName}", channel.Id, channel.Name);
            return;
        }

        if (_discord.DiscordClient.GetGuild(_discord.GuildId).GetVoiceChannel(channel.Id) != null)
        {
            Log.Debug("Skipping creating course using voice channel {channelid} {channelName}", channel.Id,
                channel.Name);
            return;
        }

        var categories = await _storage.Courses.GetCategoriesAsync();
        categories = categories.Where(x => x.AutoImportPriority >= 0).OrderByDescending(x => x.AutoImportPriority)
            .ToList();
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
                Log.Debug("Setting course position to category {category} for {channelid} {channelName}",
                    category.DiscordId, channel.Id, channel.Name);
                await channel.ModifyAsync(x => { x.CategoryId = category.DiscordId; });
                break;
            }
        }
    }

    public async Task<bool> ApplyChannelPermissionsAsync(IGuildChannel channel)
    {
        Log.Debug("Applying channel permissions for {channelid} {channelName}", channel.Id, channel.Name);
        var courseName = await _storage.Courses.GetCourseNameAsync(channel.Id);
        if (string.IsNullOrWhiteSpace(courseName))
        {
            Log.Warning("Attempted to apply permissions on channel {channelId} {channelName} for unknown course.",
                channel.Id, channel.Name);
            return false;
        }

        var guild = _discord.DiscordClient.GetGuild(_discord.GuildId);

        var verifiedRole = guild.GetRole(_verifiedRoleId);

        if (verifiedRole == null)
        {
            Log.Error("Invalid verified role ID configured in settings. Role not found.");
            return false;
        }

        Log.Debug("Everyone allow perms: {allow} Deny: {deny}", _everyoneAllowPerms, _everyoneDenyPerms);
        var everyonePerms = channel.GetPermissionOverwrite(guild.EveryoneRole);
        if (!everyonePerms.HasValue ||
            everyonePerms?.AllowValue != _everyoneAllowPerms ||
            everyonePerms?.DenyValue != _everyoneDenyPerms)
        {
            Log.Debug("Setting @everyone permissions for channel {channelId} {channelName}", channel.Id, channel.Name);
            await channel.AddPermissionOverwriteAsync(guild.EveryoneRole,
                new OverwritePermissions(_everyoneAllowPerms, _everyoneDenyPerms));
        }

        var verifiedPerms = channel.GetPermissionOverwrite(verifiedRole);
        if (!verifiedPerms.HasValue ||
            verifiedPerms?.AllowValue != _verifiedAllowPerms ||
            verifiedPerms?.DenyValue != _verifiedDenyPerms)
        {
            Log.Debug("Setting verified role permissions for channel {channelId} {channelName}", channel.Id,
                channel.Name);
            await channel.AddPermissionOverwriteAsync(verifiedRole,
                new OverwritePermissions(_verifiedAllowPerms, _verifiedDenyPerms));
        }

        foreach (var o in _rolePermissionOverrides)
            try
            {
                var role = guild.GetRole(o.RoleId);
                var perms = channel.GetPermissionOverwrite(role);
                if (!perms.HasValue ||
                    perms?.AllowValue != o.AllowedPermissions ||
                    perms?.DenyValue != o.DeniedPermissions)
                {
                    Log.Debug("Setting {role} role permissions for channel {channelId} {channelName}", o.Name,
                        channel.Id, channel.Name);
                    await Task.Delay(500);
                    await channel.AddPermissionOverwriteAsync(role,
                        new OverwritePermissions(o.AllowedPermissions, o.DeniedPermissions));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error setting {role} role permission override on course channel {message}", o.Name,
                    ex.Message);
            }

        var courseMemberIds = new HashSet<ulong>(await _storage.Courses.GetCourseUsersAsync(courseName));
        var joinedMembers = new HashSet<ulong>();
        var extraMembers = new HashSet<ulong>();

        // Add missing permissions
        foreach (var overwrite in channel.PermissionOverwrites)
            if (overwrite.TargetType == PermissionTarget.User)
            {
                joinedMembers.Add(overwrite.TargetId);
                extraMembers.Add(overwrite.TargetId);

                try
                {
                    if (overwrite.Permissions.AllowValue != _joinedAllowPerms ||
                        overwrite.Permissions.DenyValue != _joinedDenyPerms)
                    {
                        var user = _discord.DiscordClient.GetUser(overwrite.TargetId);
                        if (user == null)
                            continue;
                        Log.Information(
                            "Updating permission mismatch on channel {channemName} {channelId} for {user} {userId}",
                            channel.Name, channel.Id, user.Username, user.Id);
                        await Task.Delay(500);
                        await channel.AddPermissionOverwriteAsync(user,
                            new OverwritePermissions(_joinedAllowPerms, _joinedDenyPerms));
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to update permission mismatch for channel {channelName} {channelid}",
                        channel.Name, channel.Id);
                }
            }

        extraMembers.ExceptWith(courseMemberIds);
        courseMemberIds.ExceptWith(joinedMembers);

        Log.Debug("Found {count} new permissions for {channelid} {channelName}", courseMemberIds.Count, channel.Id,
            channel.Name);
        Log.Debug("Found {count} old permissions for {channelid} {channelName}", extraMembers.Count, channel.Id,
            channel.Name);

        foreach (var extraMember in extraMembers)
        {
            await Task.Delay(500); // To help reduce API throttling
            var user = _discord.DiscordClient.GetUser(extraMember);
            if (user == null)
                continue;
            await channel.RemovePermissionOverwriteAsync(user);
            Log.Debug("Removing permission for {user} from {channelid} {channelName}", user.Id, channel.Id,
                channel.Name);
        }

        foreach (var joinedMember in courseMemberIds)
        {
            await Task.Delay(500); // To help reduce API throttling
            var user = _discord.DiscordClient.GetUser(joinedMember);
            if (user == null)
                continue;
            await channel.AddPermissionOverwriteAsync(user,
                new OverwritePermissions(_joinedAllowPerms, _joinedDenyPerms));
            Log.Debug("Adding permission for {user} to {channelid} {channelName}", user.Id, channel.Id, channel.Name);
        }

        return true;
    }

    /// <summary>
    ///     Makes course of various different formats into the format ABCD-123
    /// </summary>
    public string NormaliseCourseName(string course)
    {
        if (DiscordChannelRegex.IsMatch(course))
            try
            {
                course = ((SocketGuildChannel)_discord.DiscordClient.GetChannel(MentionUtils.ParseChannel(course)))
                    .Name;
            }
            catch (Exception ex)
            {
                Log.Debug("Failed to parse discord channel name. {error}", ex.Message);
            }

        var match = CourseRegex.Match(course);
        if (!match.Success)
            return course.ToLower().Trim();

        return match.Groups[1].Value.ToUpper() + "-" + match.Groups[2].Value;
    }

    public async Task<bool> CanAutoCreateCourseAsync(string course)
    {
        var patterns = await _storage.Courses.GetAutoCreatePatternsAsync();
        foreach (var pattern in patterns)
            if (Regex.IsMatch(course, pattern) && _cachedCourses.ContainsKey(course))
                return true;
        return false;
    }

    public async Task<List<string>> GetAllAutoCreateCoursesAsync()
    {
        var patterns = await _storage.Courses.GetAutoCreatePatternsAsync();
        var courses = new List<string>();

        foreach (var course in _cachedCourses.Keys)
        foreach (var pattern in patterns)
            if (Regex.IsMatch(course, pattern) && _cachedCourses.ContainsKey(course))
                courses.Add(course);
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
    ///     Downloads the course list from the VUW web site and updates the local list.
    /// </summary>
    /// <returns>Boolean indicating success.</returns>
    public async Task<bool> DownloadCourseList()
    {
        try
        {
            Log.Information("Course cache download started");
            const string webListUrl = "https://service-web.wgtn.ac.nz/dotnet2/catprint.aspx?d=all";
            string[] urls =
            {
                webListUrl + "&t=u" + DateTime.Now.Year,
                webListUrl + "&t=p" + DateTime.Now.Year
            };

            var courses = new Dictionary<string, CachedCourse>();

            foreach (var url in urls)
            {
                Log.Debug("Downloading courses from: {url}", url);
                var web = new HtmlWeb();
                var document = new HtmlDocument();

                using (var client = new HttpClient())
                {
                    await using (var stream = await client.GetStreamAsync(url))
                    {
                        document.Load(stream);

                        var nodes = document.DocumentNode.SelectNodes("//p[@class='courseid']").ToArray();
                        foreach (var item in nodes)
                        {
                            var courseCode = NormaliseCourseName(item.SelectSingleNode(".//span[1]").InnerText);
                            var courseDescription = item.SelectSingleNode(".//span[2]//span[1]").InnerText;

                            if (courseDescription.StartsWith("– ")) // Remove weird dash thing from start
                                courseDescription = courseDescription.Remove(0, 2);

                            if (CourseRegex.IsMatch(courseCode))
                            {
                                if (!courses.TryAdd(courseCode, new CachedCourse(courseCode, courseDescription.Trim())))
                                    Log.Verbose("Duplicate course from download: {Course}", courseCode);
                            }
                            else
                            {
                                Log.Warning("Invalid course code from web download: {Course}", courseCode);
                            }
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
        catch
        {
            Log.Error(
                "Failed to load defaultCoursePermissions from config. Please use a valid Discord permission value. See https://discordapi.com/permissions.html");
            throw;
        }

        try
        {
            _rolePermissionOverrides = new List<RolePermissionOverride>();
            foreach (var o in _config.GetSection("courses:channelRolePermissionOverrides").GetChildren())
            {
                Log.Debug("Adding course channel role permission override for {role}", o.Key);
                _rolePermissionOverrides.Add(new RolePermissionOverride
                {
                    Name = o.Key,
                    RoleId = ulong.Parse(o["roleId"]),
                    AllowedPermissions = ulong.Parse(o["allowed"]),
                    DeniedPermissions = ulong.Parse(o["denied"])
                });
            }
        }
        catch
        {
            Log.Error(
                "Failed to load role permission overrides from config. Please use a valid Discord permission value. See https://discordapi.com/permissions.html");
            throw;
        }
    }

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

    private class RolePermissionOverride
    {
        public string Name;

        public ulong
            RoleId,
            AllowedPermissions,
            DeniedPermissions;
    }
}