using Discord.Commands;
using ECSDiscord.Modules.Util;
using ECSDiscord.Services;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ECSDiscord.BotModules
{
    [Name("Enrollments")]
    [RequireContext(ContextType.Guild)]
    public class EnrollmentsModule : ModuleBase<SocketCommandContext>
    {
        

        private readonly IConfigurationRoot _config;
        private readonly EnrollmentsService _enrollments;

        public EnrollmentsModule(IConfigurationRoot config, EnrollmentsService enrollments)
        {
            _config = config;
            _enrollments = enrollments;
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
                await ReplyAsync(errorMessage);
                return;
            }
            

            await ReplyAsync(string.Join(", ", formattedCourses));
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
                await ReplyAsync(errorMessage);
                return;
            }


            await ReplyAsync(string.Join(", ", formattedCourses));
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
                await ReplyAsync(errorMessage);
                return;
            }


            await ReplyAsync(string.Join(", ", formattedCourses));
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
            HashSet<string> invalidFormatCourses = new HashSet<string>();
            foreach (string course in courses)
            {
                string normalised = CourseService.NormaliseCourseName(course);

                if (!_enrollments.IsCourseValid(course)) // Ensure all courses are in a valid format
                    invalidFormatCourses.Add('`' + course + '`');

                if (!string.IsNullOrEmpty(normalised) && !distinctCourses.Add(normalised)) // Enrusre there are no duplicate courses
                    duplicateCourses.Add('`' + normalised + '`');
            }
            string error = "";
            if (invalidFormatCourses.Count != 0) // Error invalid courses
            {
                string s = invalidFormatCourses.Count > 1 ? "s" : "";
                string courseList = invalidFormatCourses.Aggregate((x, y) => $"{x}, {y}");
                error += $"The following courses/roles do not exist: {courseList}.\nIf you think these courses/roles should exist, please ask the \\@admins";
            }
            if (duplicateCourses.Count != 0 && !ignoreDuplicates) // Error duplicate courses
            {
                string s = duplicateCourses.Count > 1 ? "s" : "";
                string courseList = duplicateCourses.Aggregate((x, y) => $"{x}, {y}");
                error += $"\nDuplicate course{s} found: {courseList}.\nPlease ensure there are no duplicate course.";
            }
            if (invalidFormatCourses.Count != 0 || (duplicateCourses.Count != 0 && !ignoreDuplicates)) // Print then end if courses have duplicates or are invalid
            {
                errorMessage = error.Trim();
                formattedCourses = null;
                return false;
            }

            errorMessage = string.Empty;
            formattedCourses = distinctCourses;
            return true;
        }
    }
}
