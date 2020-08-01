using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ECSDiscord.Services
{
    public class EnrollmentsService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CourseService _courses;
        private readonly StorageService _storage;
        private readonly IConfigurationRoot _config;

        private ulong _guildId;
        private bool _requireVerificationToJoin;

        public enum EnrollmentResult
        {
            Success,
            CourseNotExist,
            AlreadyJoined,
            AlreadyLeft,
            Unverified,
            Failure
        }

        public EnrollmentsService(DiscordSocketClient discord, CourseService courses, StorageService storage, IConfigurationRoot config)
        {
            Log.Debug("Enrollments service loading.");
            _discord = discord;
            _courses = courses;
            _config = config;
            _storage = storage;
            loadConfig();
            Log.Debug("Enrollments service loaded.");
        }

        public async Task<EnrollmentResult> EnrollUser(string courseName, SocketUser user)
        {
            try
            {
                if (_requireVerificationToJoin)
                {
                    try
                    {
                        if(await _storage.Users.GetEncryptedUsernameAsync(user.Id) == null)
                        {
                            Log.Information("Unverified user {user} tried to join a course.", user.Id);
                            return EnrollmentResult.Unverified;
                        }
                    }
                    catch (StorageService.RecordNotFoundException)
                    {
                        Log.Information("Unverified user {user} tried to join a course.", user.Id);
                        return EnrollmentResult.Unverified;
                    }
                }

                SocketGuild guild = _discord.GetGuild(ulong.Parse(_config["guildId"]));
                await _discord.DownloadUsersAsync(new List<IGuild> { guild });

                CourseService.Course course = await IsCourseValidAsync(courseName);
                if (course == null)
                    return EnrollmentResult.CourseNotExist;

                IGuildChannel channel = guild.GetChannel(course.DiscordId);
                if(channel == null)
                {
                    Log.Error("Channel for course {course} does not exist. {discordId}", course.Code, course.DiscordId);
                    return EnrollmentResult.Failure;
                }

                if (await _storage.Users.IsUserInCourseAsync(user.Id, course.Code))
                    return EnrollmentResult.AlreadyJoined;

                if (!uint.TryParse(_config["courses:joinedUserPermissionsAllowed"], out uint allowedPermissions))
                {
                    Log.Error("Invalid joinedUserPermissionsAllowed value in config. Please configure a 32 bit integer flag permissions value. https://discordapi.com/permissions.html");
                    return EnrollmentResult.Failure;
                }
                if (!uint.TryParse(_config["courses:joinedUserPermissionsDenied"], out uint deniedPermissions))
                {
                    Log.Error("Invalid joinedUserPermissionsDenied value in config. Please configure a 32 bit integer flag permissions value. https://discordapi.com/permissions.html");
                    return EnrollmentResult.Failure;
                }

                await _storage.Users.EnrollUserAsync(user.Id, course.Code);
                //await channel.AddPermissionOverwriteAsync(user, new OverwritePermissions(allowedPermissions, deniedPermissions));
                await applyChannelPermissionsAsync(channel);
                return EnrollmentResult.Success;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to enrol user in course {course} {message}", courseName, ex.Message);
                return EnrollmentResult.Failure;
            }
        }

        public async Task<EnrollmentResult> DisenrollUser(string courseName, SocketUser user)
        {
            try
            {
                SocketGuild guild = _discord.GetGuild(ulong.Parse(_config["guildId"]));
                await _discord.DownloadUsersAsync(new List<IGuild> { guild });

                CourseService.Course course = await IsCourseValidAsync(courseName);
                if (course == null)
                    return EnrollmentResult.CourseNotExist;

                if(!await _storage.Users.IsUserInCourseAsync(user.Id, course.Code))
                    return EnrollmentResult.AlreadyLeft;

                IGuildChannel channel = guild.GetChannel(course.DiscordId);
                if (channel == null)
                {
                    Log.Error("Course channel does not exist {course}", course.Code);
                    return EnrollmentResult.Failure;
                }

                await _storage.Users.DisenrollUserAsync(user.Id, course.Code);

                //await channel.RemovePermissionOverwriteAsync(user);
                await applyChannelPermissionsAsync(channel);
                return EnrollmentResult.Success;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to disenroll user from course {course} {message}", courseName, ex.Message);
                return EnrollmentResult.Failure;
            }
        }

        public async Task<List<string>> GetUserCourses(SocketUser user)
        {
            return await _storage.Users.GetUserCoursesAsync(user.Id);
        }

        public async Task<IList<SocketUser>> GetCourseMembers(string course)
        {
            return (await _storage.Courses.GetCourseUsersAsync(_courses.NormaliseCourseName(course))).Select(x => _discord.GetUser(x)).ToList();
        }

        public async Task<CourseService.Course> IsCourseValidAsync(string name)
        {
            if (await _courses.CourseExists(name))
            {
                return await _courses.GetCourse(name);
            }
            string normalisedName = _courses.NormaliseCourseName(name);
            if (await _courses.CourseExists(normalisedName))
            {
                return await _courses.GetCourse(normalisedName);
            }
            return null;
        }

        private async Task applyChannelPermissionsAsync(IGuildChannel channel)
        {
            
        }

        private void loadConfig()
        {
            _guildId = ulong.Parse(_config["guildId"]);
            if(!bool.TryParse(_config["courses:requireVerificationToJoin"], out _requireVerificationToJoin))
            {
                Log.Error("Invalid boolean for requireVerificationToJoin setting.");
                throw new Exception("Invalid boolean for requireVerificationToJoin setting.");
            }
        }
    }
}
