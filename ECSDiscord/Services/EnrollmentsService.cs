using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ECSDiscord.Services
{
    public class EnrollmentsService
    {
        private static readonly Regex CourseRegex = new Regex("([A-Za-z]{4})[ -_]?([0-9]{3})"); // Pattern for course names

        private readonly DiscordSocketClient _discord;
        private readonly IConfigurationRoot _config;
        
        public enum EnrollmentResult
        {
            Success,
            CourseNotExist,
            AlreadyJoined,
            AlreadyLeft,
            Failure
        }

        public EnrollmentsService(DiscordSocketClient discord, IConfigurationRoot config)
        {
            _discord = discord;
            _config = config;
        }

        public async Task<EnrollmentResult> EnrollUser(string course, SocketGuildUser user)
        {
            return EnrollmentResult.Failure;
        }

        public async Task<EnrollmentResult> DisenrollUser(string course, SocketGuildUser user)
        {
            return EnrollmentResult.Failure;
        }

        public async Task<List<string>> GetCourses(SocketGuildUser user)
        {
            return null;
        }

        public static string NormaliseCourseName(string course)
        {
            Match match = CourseRegex.Match(course);
            if (!match.Success)
                return string.Empty;

            return match.Groups[1].Value.ToUpper() + "-" + match.Groups[2].Value;
        }

        public static bool IsCourseValid(string course)
        {
            return CourseRegex.IsMatch(course);
        }
    }
}
