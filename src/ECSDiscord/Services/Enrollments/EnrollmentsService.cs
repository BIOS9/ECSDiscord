using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ECSDiscord.Services.Bot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECSDiscord.Services.Enrollments;
using ECSDiscord.Services.Storage;
using Microsoft.Extensions.Options;

namespace ECSDiscord.Services
{
    public class EnrollmentsService : IHostedService
    {
        private readonly EnrollmentsOptions _options;
        private readonly DiscordBot _discord;
        private readonly CourseService _courses;
        private readonly StorageService _storage;
        private readonly VerificationService _verification;

        public enum EnrollmentResult
        {
            Success,
            CourseNotExist,
            AlreadyJoined,
            AlreadyLeft,
            Unverified,
            Blacklisted,
            Failure
        }

        public EnrollmentsService(IOptions<EnrollmentsOptions> options, DiscordBot discordBot, CourseService courses, StorageService storage, VerificationService verification, IConfiguration config)
        {
            Log.Debug("Enrollments service loading.");
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _discord = discordBot;
            _courses = courses;
            _storage = storage;
            _verification = verification;
            
            Log.Debug("Enrollments service loaded.");
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _discord.DiscordClient.UserJoined += _discord_UserJoined;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _discord.DiscordClient.UserJoined -= _discord_UserJoined;
            return Task.CompletedTask;
        }

        private async Task _discord_UserJoined(SocketGuildUser arg)
        {
            try
            {
                await ApplyUserCoursePermissions(arg);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error re-adding user to courses after leave then join. {message}", ex.Message);
            }
        }

        public async Task<bool> RequiresVerification(SocketUser user)
        {
            if (!_options.RequireVerificationToJoin)
                return false;

            try
            {
                return !await _verification.IsUserVerifiedAsync(user);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking user verification {user} requirement.", user.Id);
                throw;
            }
        }

        public async Task<EnrollmentResult> EnrollUser(string courseName, SocketUser user)
        {
            try
            {
                if (_options.RequireVerificationToJoin)
                {
                    try
                    {
                        if (!await _verification.IsUserVerifiedAsync(user))
                        {
                            Log.Information("Unverified user {user} tried to join a course.", user.Id);
                            return EnrollmentResult.Unverified;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error checking user verification {user} while joining a course.", user.Id);
                        return EnrollmentResult.Failure;
                    }
                }

                if (await _storage.Users.IsDisallowCourseJoinSetAsync(user.Id))
                {
                    return EnrollmentResult.Blacklisted;
                }

                SocketGuild guild = _discord.DiscordClient.GetGuild(_discord.GuildId);
                await _discord.DiscordClient.DownloadUsersAsync(new List<IGuild> { guild });

                StorageService.CourseStorage.CourseAlias alias = await _storage.Courses.GetAliasAsync(_courses.NormaliseCourseName(courseName));
                Console.WriteLine(alias == null);
                if (alias != null)
                {
                    courseName = alias.Target;
                }

                CourseService.Course course = await IsCourseValidAsync(courseName);
                if (course == null)
                {
                    // Create course if does not exist yet but is allowed.
                    string normalisedName = _courses.NormaliseCourseName(courseName);
                    if (await _courses.CanAutoCreateCourseAsync(normalisedName))
                    {
                        Log.Information("Auto creating course {course}", normalisedName);
                        await _courses.CreateCourseAsync(normalisedName);
                        course = await IsCourseValidAsync(courseName);
                        if (course == null)
                            return EnrollmentResult.CourseNotExist;
                    }
                    else
                    {
                        return EnrollmentResult.CourseNotExist;
                    }
                }

                IGuildChannel channel = guild.GetChannel(course.DiscordId);
                if (channel == null)
                {
                    Log.Error("Channel for course {course} does not exist. {discordId}", course.Code, course.DiscordId);
                    return EnrollmentResult.Failure;
                }

                if (await _storage.Users.IsUserInCourseAsync(user.Id, course.Code))
                    return EnrollmentResult.AlreadyJoined;

                await _storage.Users.EnrollUserAsync(user.Id, course.Code);
                await _courses.ApplyChannelPermissionsAsync(channel);
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
                SocketGuild guild = _discord.DiscordClient.GetGuild(_discord.GuildId);
                await _discord.DiscordClient.DownloadUsersAsync(new List<IGuild> { guild });

                CourseService.Course course = await IsCourseValidAsync(courseName);
                if (course == null)
                {
                    string normalisedName = _courses.NormaliseCourseName(courseName);
                    if (await _courses.CanAutoCreateCourseAsync(normalisedName))
                        return EnrollmentResult.AlreadyLeft; // Course has not been auto created yet so user cannot be in it.
                    return EnrollmentResult.CourseNotExist;
                }

                if (!await _storage.Users.IsUserInCourseAsync(user.Id, course.Code))
                    return EnrollmentResult.AlreadyLeft;

                IGuildChannel channel = guild.GetChannel(course.DiscordId);
                if (channel == null)
                {
                    Log.Error("Course channel does not exist {course}", course.Code);
                    return EnrollmentResult.Failure;
                }

                await _storage.Users.DisenrollUserAsync(user.Id, course.Code);
                await _courses.ApplyChannelPermissionsAsync(channel);
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
            return (await _storage.Courses.GetCourseUsersAsync(_courses.NormaliseCourseName(course))).Select(x => _discord.DiscordClient.GetUser(x)).Where(x => x != null).ToList();
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

        public async Task ApplyUserCoursePermissions(SocketUser user)
        {
            Log.Information("Applying permission for all courses of user {user}#{discriminator} {discordId}", user.Username, user.Discriminator, user.Id);
            SocketGuild guild = _discord.DiscordClient.GetGuild(_discord.GuildId);
            List<string> courses = await GetUserCourses(user);
            foreach(string courseName in courses)
            {
                try
                {
                    CourseService.Course course = await IsCourseValidAsync(courseName);
                    if (course == null) continue;
                    IGuildChannel channel = guild.GetChannel(course.DiscordId);
                    if (channel == null)
                    {
                        Log.Error("Channel for course {course} does not exist. {discordId}", course.Code, course.DiscordId);
                        continue;
                    }
                    await _courses.ApplyChannelPermissionsAsync(channel);
                }
                catch(Exception ex)
                {
                    Log.Error(ex, "Failed to apply permissions for user course {user}#{discriminator} {discordId}, {course}, {message}", user.Username, user.Discriminator, user.Id, courseName, ex.Message);
                }
            }            
        }
    }
}
