using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECSDiscord.Services
{
    public class EnrollmentsService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CourseService _courses;
        private readonly IConfigurationRoot _config;

        public enum EnrollmentResult
        {
            Success,
            CourseNotExist,
            AlreadyJoined,
            AlreadyLeft,
            Failure
        }

        public EnrollmentsService(DiscordSocketClient discord, CourseService courses, IConfigurationRoot config)
        {
            _discord = discord;
            _courses = courses;
            _config = config;
        }

        public async Task<EnrollmentResult> EnrollUser(string course, SocketUser user)
        {
            try
            {
                SocketGuild guild = _discord.GetGuild(ulong.Parse(_config["guildId"]));
                await _discord.DownloadUsersAsync(new List<IGuild> { guild });

                if (!IsCourseValid(course, out _))
                    return EnrollmentResult.CourseNotExist;

                IGuildChannel channel = await _courses.GetOrCreateChannel(course);
                if (channel.GetPermissionOverwrite(user).HasValue)
                    return EnrollmentResult.AlreadyJoined;

                if (!uint.TryParse(_config["joinedUserPermissionsAllowed"], out uint allowedPermissions))
                {
                    Log.Error("Invalid joinedUserPermissionsAllowed value in config. Please configure a 32 bit integer flag permissions value. https://discordapi.com/permissions.html");
                    return EnrollmentResult.Failure;
                }
                if (!uint.TryParse(_config["joinedUserPermissionsDenied"], out uint deniedPermissions))
                {
                    Log.Error("Invalid joinedUserPermissionsDenied value in config. Please configure a 32 bit integer flag permissions value. https://discordapi.com/permissions.html");
                    return EnrollmentResult.Failure;
                }
                await channel.AddPermissionOverwriteAsync(user, new OverwritePermissions(allowedPermissions, deniedPermissions));
                return EnrollmentResult.Success;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to enrol user in course {course} {message}", course, ex.Message);
                return EnrollmentResult.Failure;
            }
        }

        public async Task<EnrollmentResult> DisenrollUser(string course, SocketUser user)
        {
            try
            {
                SocketGuild guild = _discord.GetGuild(ulong.Parse(_config["guildId"]));
                await _discord.DownloadUsersAsync(new List<IGuild> { guild });

                if (!IsCourseValid(course, out CourseService.Course courseObject))
                    return EnrollmentResult.CourseNotExist;

                IGuildChannel channel = _courses.GetChannel(course);
                if (channel == null || !channel.GetPermissionOverwrite(user).HasValue)
                    return EnrollmentResult.AlreadyLeft;

                await channel.RemovePermissionOverwriteAsync(user);

                
                if (courseObject.AutoDelete && !channel.PermissionOverwrites.Any(x => x.TargetType == PermissionTarget.User && x.TargetId != guild.EveryoneRole.Id))
                {
                    Log.Information("Deleting course channel that has no users {channel}", course);
                    await channel.DeleteAsync();
                }
                return EnrollmentResult.Success;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to disenroll user from course {course} {message}", course, ex.Message);
                return EnrollmentResult.Failure;
            }
        }

        public List<string> GetUserCourses(SocketUser user)
        {
            SocketGuild guild = _discord.GetGuild(ulong.Parse(_config["guildId"]));
            return guild.TextChannels.Where(x => IsCourseValid(x.Name, out _) && x.PermissionOverwrites.Any(p => p.TargetId == user.Id)).Select(x => _courses.NormaliseCourseName(x.Name)).ToList();
        }

        public bool IsCourseValid(string name, out CourseService.Course course)
        {
            if (_courses.CourseExists(name))
            {
                course = _courses.GetCourse(name);
                return true;
            }
            string normalisedName = _courses.NormaliseCourseName(name);
            if (_courses.CourseExists(normalisedName))
            {
                course = _courses.GetCourse(normalisedName);
                return true;
            }
            course = null;
            return false;
        }
    }
}
