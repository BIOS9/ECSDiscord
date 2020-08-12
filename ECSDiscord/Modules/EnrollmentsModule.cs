using Discord.Commands;
using ECSDiscord.Util;
using ECSDiscord.Services;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ECSDiscord.Services.EnrollmentsService;
using Discord;
using Discord.WebSocket;
using ECSDiscord.Core.Translations;

namespace ECSDiscord.BotModules
{
    [Name("Enrollments")]
    public class EnrollmentsModule : ModuleBase<SocketCommandContext>
    {
        private readonly IConfigurationRoot _config;
        private readonly ITranslator _translator;
        private readonly EnrollmentsService _enrollments;
        private readonly CourseService _courses;

        public EnrollmentsModule(IConfigurationRoot config, ITranslator translator, EnrollmentsService enrollments, CourseService courses)
        {
            _config = config;
            _translator = translator;
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

            await ReplyAsync(_translator.T("COMMAND_PROCESSING"));
            // Add user to courses
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string course in formattedCourses)
            {
                EnrollmentResult result = await _enrollments.EnrollUser(course, Context.User);
                switch(result)
                {
                    case EnrollmentResult.AlreadyJoined:
                        stringBuilder.Append(_translator.T("ENROLLMENT_ALREADY_ENROLLED", course));
                        break;
                    case EnrollmentResult.CourseNotExist:
                        stringBuilder.Append(_translator.T("ENROLLMENT_INVALID_COURSE", course));
                        break;
                    default:
                    case EnrollmentResult.Failure:
                        stringBuilder.Append(_translator.T("ENROLLMENT_SERVER_ERROR", course));
                        break;
                    case EnrollmentResult.Success:
                        stringBuilder.Append(_translator.T("ENROLLMENT_JOIN_SUCCESS", course));
                        break;
                    case EnrollmentResult.Unverified:
                        stringBuilder.Append(_translator.T("ENROLLMENT_VERIFICATION_REQUIRED", course));
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

            if(courses.Length == 1 && courses[0].Equals("all", System.StringComparison.OrdinalIgnoreCase))
            {
                await LeaveAllAsync();
                return;
            }

            // Ensure course list is valid
            if (!checkCourses(courses, true, out string errorMessage, out ISet<string> formattedCourses))
            {
                await ReplyAsync(errorMessage.SanitizeMentions());
                return;
            }

            await ReplyAsync(_translator.T("COMMAND_PROCESSING"));
            // Add user to courses
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string course in formattedCourses)
            {
                EnrollmentResult result = await _enrollments.DisenrollUser(course, Context.User);
                switch (result)
                {
                    case EnrollmentResult.AlreadyLeft:
                        stringBuilder.Append(_translator.T("ENROLLMENT_ALREADY_LEFT", course));
                        break;
                    case EnrollmentResult.CourseNotExist:
                        stringBuilder.Append(_translator.T("ENROLLMENT_INVALID_COURSE", course));
                        break;
                    default:
                    case EnrollmentResult.Failure:
                        stringBuilder.Append(_translator.T("ENROLLMENT_SERVER_ERROR", course));
                        break;
                    case EnrollmentResult.Success:
                        stringBuilder.Append(_translator.T("ENROLLMENT_LEAVE_SUCCESS", course));
                        break;
                }
            }

            await ReplyAsync(stringBuilder.ToString().Trim().SanitizeMentions());
        }

        [Command("leaveall")]
        [Alias("disenrolall", "disenrollall")]
        [Summary("Removes you from all courses.")]
        public async Task LeaveAllAsync()
        {
            if (!Context.CheckConfigChannel("enrollments", _config)) return;

            List<string> courses = await _enrollments.GetUserCourses(Context.User);
            if (courses.Count == 0)
            {
                await ReplyAsync(_translator.T("ENROLLMENT_NO_COURSES_JOINED"));
                return;
            }

            await ReplyAsync(_translator.T("COMMAND_PROCESSING"));
            // Add user to courses
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string course in courses)
            {
                EnrollmentResult result = await _enrollments.DisenrollUser(course, Context.User);
                switch (result)
                {
                    case EnrollmentResult.AlreadyLeft:
                        stringBuilder.Append(_translator.T("ENROLLMENT_ALREADY_LEFT", course));
                        break;
                    case EnrollmentResult.CourseNotExist:
                        stringBuilder.Append(_translator.T("ENROLLMENT_INVALID_COURSE", course));
                        break;
                    default:
                    case EnrollmentResult.Failure:
                        stringBuilder.Append(_translator.T("ENROLLMENT_SERVER_ERROR", course));
                        break;
                    case EnrollmentResult.Success:
                        stringBuilder.Append(_translator.T("ENROLLMENT_LEAVE_SUCCESS", course));
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
            if (!checkCourses(courses, true, out string errorMessage, out ISet<string> formattedCourses))
            {
                await ReplyAsync(errorMessage.SanitizeMentions());
                return;
            }

            await ReplyAsync(_translator.T("COMMAND_PROCESSING"));

            List<string> existingCourses = await _enrollments.GetUserCourses(Context.User); // List of courses the user is already in, probably should've used a set for that

            // Add user to courses
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string course in formattedCourses)
            {
                bool alreadyInCourse = existingCourses.Contains(course);
                EnrollmentResult result = alreadyInCourse ?
                    await _enrollments.DisenrollUser(course, Context.User) :
                    await _enrollments.EnrollUser(course, Context.User);

                switch (result)
                {
                    case EnrollmentResult.CourseNotExist:
                        stringBuilder.Append(_translator.T("ENROLLMENT_INVALID_COURSE", course));
                        break;
                    default:
                    case EnrollmentResult.Failure:
                        stringBuilder.Append(_translator.T("ENROLLMENT_SERVER_ERROR", course));
                        break;
                    case EnrollmentResult.Success:
                        if(alreadyInCourse)
                            stringBuilder.Append(_translator.T("ENROLLMENT_LEAVE_SUCCESS", course));
                        else
                            stringBuilder.Append(_translator.T("ENROLLMENT_JOIN_SUCCESS", course));
                        break;
                    case EnrollmentResult.Unverified:
                        stringBuilder.Append(_translator.T("ENROLLMENT_VERIFICATION_REQUIRED", course));
                        break;
                }
            }

            await ReplyAsync(stringBuilder.ToString().Trim().SanitizeMentions());
        }

        [Command("listcourses")]
        [Alias("list", "courses", "ranks", "roles", "papers")]
        [Summary("List the courses you are in.")]
        public async Task CoursesAsync()
        {
            if (!Context.CheckConfigChannel("enrollments", _config)) return; // Ensure command is only executed in allowed channels

            List<string> courses = await _enrollments.GetUserCourses(Context.User);
            if (courses.Count == 0)
                await ReplyAsync($"You are not in any courses. Use `{_config["prefix"]}allcourses` to view a list of all courses.");
            else
                await ReplyAsync("You are in the following courses:\n" +
                    courses
                    .Select(x => $"`{x}`")
                    .Aggregate((x, y) => $"{x}, {y}")
                    .SanitizeMentions());
        }

        [Command("listcourses")]
        [Alias("list", "courses", "ranks", "roles", "papers")]
        [Summary("List the courses a user is in.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task CoursesAsync(SocketUser user)
        {
            List<string> courses = await _enrollments.GetUserCourses(user);
            if (courses.Count == 0)
                await ReplyAsync($"That user is not in any courses.");
            else
                await ReplyAsync("That user is in the following courses:\n" +
                    courses
                    .Select(x => $"`{x}`")
                    .Aggregate((x, y) => $"{x}, {y}")
                    .SanitizeMentions());
        }

        [Command("members")]
        [Alias("coursemembers")]
        [Summary("Lists the members in a course.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task MembersAsync(string courseName)
        {
            await ReplyAsync(_translator.T("COMMAND_PROCESSING"));
            if (!await _courses.CourseExists(courseName))
            {
                await ReplyAsync(_translator.T("INVALID_COURSE"));
                return;
            }

            IList<SocketUser> users = await _enrollments.GetCourseMembers(courseName);
            if(users == null || users.Count == 0)
            {
                await ReplyAsync(_translator.T("COURSE_EMPTY"));
                return;
            }

            StringBuilder builder = new StringBuilder(_courses.NormaliseCourseName(courseName) + $" has the following {users.Count} members:```");
            foreach(SocketUser user in users)
            {
                builder.Append("\n");
                builder.Append($"{user.Username}#{user.Discriminator}  -  {user.Id}");
            }
            await ReplyAsync(builder.ToString().SanitizeMentions() + "```");
        }

        [Command("membercount")]
        [Alias("countmembers", "coursemembercount")]
        [Summary("Gives the number of members in a course.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task MemberCountAsync(string courseName)
        {
            await ReplyAsync(_translator.T("COMMAND_PROCESSING"));
            if (!await _courses.CourseExists(courseName))
            {
                await ReplyAsync(_translator.T("INVALID_COURSE"));
                return;
            }

            IList<SocketUser> users = await _enrollments.GetCourseMembers(courseName);
            if (users == null || users.Count == 0)
            {
                await ReplyAsync(_translator.T("COURSE_EMPTY"));
                return;
            }

            await ReplyAsync(_courses.NormaliseCourseName(courseName) + $" has {users.Count} members.".SanitizeMentions());
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
                string normalised = _courses.NormaliseCourseName(course);

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
    }
}
