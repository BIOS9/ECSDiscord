using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ECSDiscord.Services
{
    public class CourseService
    {
        public class Course
        {
            public readonly string Name;
            public readonly string Description;

            public Course(string name, string description)
            {
                Name = name;
                Description = description;
            }
        }

        private static readonly Regex CourseRegex = new Regex("([A-Za-z]{4})[ -_]?([0-9]{3})"); // Pattern for course names
        private readonly IConfigurationRoot _config;
        private Dictionary<string, Course> _courses = new Dictionary<string, Course>
        {
            { "COMP-102", new Course("COMP-102", "Programming blah blah") },
            { "COMP-103", new Course("COMP-103", "Programming blah blah") },
            { "ENGR-112", new Course("ENGR-112", "Programming blah blah") },
            { "ENGR-123", new Course("ENGR-123", "Programming blah blah") },
        };

        public CourseService(IConfigurationRoot config)
        {
            _config = config;
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
    }
}
