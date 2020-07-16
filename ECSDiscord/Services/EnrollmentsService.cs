using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
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
            if (!IsCourseValid(course))
                return EnrollmentResult.CourseNotExist;


            return EnrollmentResult.Success;
        }

        public async Task<EnrollmentResult> DisenrollUser(string course, SocketUser user)
        {
            return EnrollmentResult.Failure;
        }

        public async Task<List<string>> GetUserCourses(SocketUser user)
        {
            return null;
        }        

        public bool IsCourseValid(string course)
        {
            return _courses.CourseExists(course) || _courses.CourseExists(CourseService.NormaliseCourseName(course));
        }
    }
}
