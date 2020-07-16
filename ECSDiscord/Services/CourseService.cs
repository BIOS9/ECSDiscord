using Discord.WebSocket;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ECSDiscord.Services
{
    public class CourseService
    {
        public class Course
        {
            public readonly string Code;
            public readonly string Description;

            public Course(string code, string description)
            {
                Code = code;
                Description = description;
            }
        }

        private static readonly Regex CourseRegex = new Regex("([A-Za-z]{4})[ -_]?([0-9]{3})"); // Pattern for course names

        private readonly IConfigurationRoot _config;
        private readonly DiscordSocketClient _discord;

        private Dictionary<string, Course> _courses = new Dictionary<string, Course>();

        public CourseService(IConfigurationRoot config, DiscordSocketClient discord)
        {
            _config = config;
            _discord = discord;
            DownloadCourseList().Wait();
        }

        public IList<Course> GetCourses()
        {
            return _courses.Values.ToList();
        }

        public Course GetCourse(string course)
        {
            return _courses[course];
        }

        public bool CourseExists(string course)
        {
            return _courses.ContainsKey(course);
        }

        public static string NormaliseCourseName(string course)
        {
            Match match = CourseRegex.Match(course);
            if (!match.Success)
                return string.Empty;

            return match.Groups[1].Value.ToUpper() + "-" + match.Groups[2].Value;
        }

        public async Task DownloadCourseList()
        {
            try
            {
                Log.Information("Course download started");
                const string webListUrl = "https://service-web.wgtn.ac.nz/dotnet2/catprint.aspx?d=all";
                string[] urls = new string[]
                {
                webListUrl + "&t=u" + DateTime.Now.Year,
                webListUrl + "&t=p" + DateTime.Now.Year
                };

                Dictionary<string, Course> courses = new Dictionary<string, Course>();

                foreach (string url in urls)
                {
                    Log.Debug("Downloading courses from: {url}", url);
                    HtmlWeb web = new HtmlWeb();
                    HtmlDocument document = await web.LoadFromWebAsync(url);

                    HtmlNode[] nodes = document.DocumentNode.SelectNodes("//p[@class='courseid']").ToArray();
                    foreach (HtmlNode item in nodes)
                    {
                        string courseCode = NormaliseCourseName(item.SelectSingleNode(".//span[1]").InnerText);
                        string courseDescription = item.SelectSingleNode(".//span[2]//span[1]").InnerText;

                        if (courseDescription.StartsWith("– ")) // Remove weird dash thing from start
                            courseDescription = courseDescription.Remove(0, 2);

                        if (CourseRegex.IsMatch(courseCode))
                        {
                            if (!courses.TryAdd(courseCode, new Course(courseCode, courseDescription.Trim())))
                                Log.Debug("Duplicate course from download: {course}", courseCode);
                        }
                        else
                            Log.Warning("Invalid course code from web download: {course}", courseCode);
                    }
                }
                Log.Information("Course download finished");
                _courses = courses; // Atomic update of courses
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Course download failed: {message}. No changes made.", ex.Message);
            }
        }
    }
}
