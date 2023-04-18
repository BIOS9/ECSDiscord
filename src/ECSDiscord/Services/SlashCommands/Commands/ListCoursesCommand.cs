using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using ECSDiscord.Services.Translations;

namespace ECSDiscord.Services.SlashCommands.Commands;

public class ListCoursesCommand : ISlashCommand
{
    private readonly CourseService _courseService;
    private readonly ITranslator _translator;

    public ListCoursesCommand(CourseService courseService, ITranslator translator)
    {
        _courseService = courseService ?? throw new ArgumentNullException(nameof(courseService));
        _translator = translator ?? throw new ArgumentNullException(nameof(translator));
    }

    public string Name => "listcourses";

    public SlashCommandProperties Build()
    {
        return new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("List all available courses.")
            .Build();
    }

    public async Task ExecuteAsync(ISlashCommandInteraction command)
    {
        var courseNames = new HashSet<string>();
        courseNames.UnionWith(await _courseService.GetAllAutoCreateCoursesAsync());
        courseNames.UnionWith((await _courseService.GetCourses()).Select(x => x.Code));
        courseNames.UnionWith((await _courseService.GetAllAliasesAsync()).Where(x => !x.Hidden).Select(x => x.Name));

        if (courseNames.Count == 0)
        {
            await command.RespondAsync(_translator.T("NO_COURSES"), ephemeral: true);
            return;
        }

        var level100Pattern = new Regex("[a-z]{4}-1[0-9]{2}", RegexOptions.IgnoreCase);
        var level100Courses = courseNames.Where(x => level100Pattern.IsMatch(x)).ToHashSet();

        var level200Pattern = new Regex("[a-z]{4}-2[0-9]{2}", RegexOptions.IgnoreCase);
        var level200Courses = courseNames.Where(x => level200Pattern.IsMatch(x)).ToHashSet();

        var level300Pattern = new Regex("[a-z]{4}-3[0-9]{2}", RegexOptions.IgnoreCase);
        var level300Courses = courseNames.Where(x => level300Pattern.IsMatch(x)).ToHashSet();

        var level400Pattern = new Regex("[a-z]{4}-4[0-9]{2}", RegexOptions.IgnoreCase);
        var level400Courses = courseNames.Where(x => level400Pattern.IsMatch(x)).ToHashSet();

        var otherCourses = new HashSet<string>(courseNames);
        otherCourses.ExceptWith(level100Courses);
        otherCourses.ExceptWith(level200Courses);
        otherCourses.ExceptWith(level300Courses);
        otherCourses.ExceptWith(level400Courses);

        string CreateCourseBlock(ICollection<string> courses)
        {
            var courseList = courses.ToList();
            courseList.Sort();

            var stringBuilder = new StringBuilder("```\n");
            var count = 1;
            foreach (var c in courseList)
            {
                stringBuilder.Append(c);
                stringBuilder.Append(count % 4 == 0 ? '\n' : '\t');
            }

            stringBuilder.Append("\n```");
            return stringBuilder.ToString();
        }

        // Credit to VicBot for this style of course listing tinyurl.com/VicBot
        var embedBuilder = new EmbedBuilder();
        embedBuilder.WithTitle("Courses");
        embedBuilder.AddField("100-Level", CreateCourseBlock(level100Courses));
        embedBuilder.AddField("200-Level", CreateCourseBlock(level200Courses));
        embedBuilder.AddField("300-Level", CreateCourseBlock(level300Courses));
        embedBuilder.AddField("400-Level", CreateCourseBlock(level400Courses));
        embedBuilder.AddField("Other", CreateCourseBlock(otherCourses));
        embedBuilder.WithColor(new Color(15, 84, 53));

        await command.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
    }
}