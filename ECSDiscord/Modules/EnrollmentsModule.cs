﻿using Discord.Commands;
using ECSDiscord.Modules.Util;
using ECSDiscord.Services;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static ECSDiscord.Services.EnrollmentsService;

namespace ECSDiscord.BotModules
{
    [Name("Enrollments")]
    [RequireContext(ContextType.Guild)]
    public class EnrollmentsModule : ModuleBase<SocketCommandContext>
    {
        

        private readonly IConfigurationRoot _config;
        private readonly EnrollmentsService _enrollments;
        private readonly CourseService _courses;

        public EnrollmentsModule(IConfigurationRoot config, EnrollmentsService enrollments, CourseService courses)
        {
            _config = config;
            _enrollments = enrollments;
            _courses = courses;
        }

        [Command("join")]
        [Alias("enroll", "enrol")]
        [Summary("Join a uni course channel.")]
        public async Task JoinAsync(params string[] courses)
        {
            // Ensure command is only executed in allowed channels
            if (!Context.CheckConfigChannel("enrollments", _config)) return;

            // Ensure course list is valid
            if (!checkCourses(courses, true, out string errorMessage, out ISet<string> formattedCourses))
            {
                await ReplyAsync(errorMessage.SanitizeMentions());
                return;
            }

            await checkUpdating();

            // Add user to courses
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string course in formattedCourses)
            {
                EnrollmentResult result = await _enrollments.EnrollUser(course, Context.User);
                switch(result)
                {
                    case EnrollmentResult.AlreadyJoined:
                        stringBuilder.Append($":warning:  **{course}** - You are already in `{course}`\n");
                        break;
                    case EnrollmentResult.CourseNotExist:
                        stringBuilder.Append($":x:  **{course}** - The course `{course}` does not exist\n");
                        break;
                    case EnrollmentResult.Failure:
                        stringBuilder.Append($":fire:  **{course}** - A server error occured. Please ask and admin to check the logs.\n");
                        break;
                    case EnrollmentResult.Success:
                        stringBuilder.Append($":white_check_mark:  **{course}** - Added you to {course} successfully.\n");
                        break;
                }
            }

            await ReplyAsync(stringBuilder.ToString().Trim().SanitizeMentions());
        }

        [Command("leave")]
        [Alias("unenroll", "unenrol", "disenroll", "disenrol")]
        [Summary("Leave a uni course channel.")]
        public async Task LeaveAsync(params string[] courses)
        {
            // Ensure command is only executed in allowed channels
            if (!Context.CheckConfigChannel("enrollments", _config)) return;

            // Ensure course list is valid
            if (!checkCourses(courses, true, out string errorMessage, out ISet<string> formattedCourses))
            {
                await ReplyAsync(errorMessage.SanitizeMentions());
                return;
            }

            await checkUpdating();


            // Add user to courses
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string course in formattedCourses)
            {
                EnrollmentResult result = await _enrollments.DisenrollUser(course, Context.User);
                switch (result)
                {
                    case EnrollmentResult.AlreadyLeft:
                        stringBuilder.Append($":warning:  **{course}** - You are not in `{course}`\n");
                        break;
                    case EnrollmentResult.CourseNotExist:
                        stringBuilder.Append($":x:  **{course}** - The course `{course}` does not exist\n");
                        break;
                    case EnrollmentResult.Failure:
                        stringBuilder.Append($":fire:  **{course}** - A server error occured. Please ask and admin to check the logs.\n");
                        break;
                    case EnrollmentResult.Success:
                        stringBuilder.Append($":white_check_mark:  **{course}** - Removed you from {course} successfully.\n");
                        break;
                }
            }

            await ReplyAsync(stringBuilder.ToString().Trim().SanitizeMentions());
        }

        [Command("togglecourse")]
        [Alias("rank", "role", "course", "paper", "disenroll", "disenrol")]
        [Summary("Join or leave a uni course channel.")]
        public async Task ToggleCourseAsync(params string[] courses)
        {
            // Ensure command is only executed in allowed channels
            if (!Context.CheckConfigChannel("enrollments", _config)) return;

            // Ensure course list is valid
            if (!checkCourses(courses, false, out string errorMessage, out ISet<string> formattedCourses))
            {
                await ReplyAsync(errorMessage.SanitizeMentions());
                return;
            }

            await checkUpdating();

            await ReplyAsync(string.Join(", ", formattedCourses).SanitizeMentions());
        }

        [Command("courses")]
        [Alias("list")]
        [Summary("List the courses you are in.")]
        public async Task CoursesAsync()
        {
            if (!Context.CheckConfigChannel("enrollments", _config)) return; // Ensure command is only executed in allowed channels
            //ReplyAsync
        }

        private bool checkCourses(string[] courses, bool ignoreDuplicates, out string errorMessage, out ISet<string> formattedCourses)
        {
            // Ensure courses are provided by user
            if (courses == null || courses.Length == 0)
            {
                errorMessage = "Please specify one or more courses to join (separated by spaces) e.g\n```" +
                      _config["prefix"] + "join comp102 engr101```";
                formattedCourses = null;
                return false;
            }

            HashSet<string> distinctCourses = new HashSet<string>();
            HashSet<string> duplicateCourses = new HashSet<string>();
            
            foreach (string course in courses)
            {
                string normalised = CourseService.NormaliseCourseName(course);

                if (!string.IsNullOrEmpty(normalised) && !distinctCourses.Add(normalised)) // Enrusre there are no duplicate courses
                    duplicateCourses.Add('`' + normalised + '`');
            }

            if (duplicateCourses.Count != 0 && !ignoreDuplicates) // Error duplicate courses
            {
                string s = duplicateCourses.Count > 1 ? "s" : "";
                string courseList = duplicateCourses.Aggregate((x, y) => $"{x}, {y}");
                errorMessage = $"\nDuplicate course{s} found: {courseList}.Please ensure there are no duplicate course.";;
                formattedCourses = null;
                return false;
            }

            errorMessage = string.Empty;
            formattedCourses = distinctCourses;
            return true;
        }

        /// <summary>
        /// Checks if the course service is currently updating and informs the user.
        /// </summary>
        private async Task checkUpdating()
        {
            if (_courses.UpdatingCourses)
                await ReplyAsync("Courses are currently being updated.\nYour request will be processed shortly.");
        }
    }
}
