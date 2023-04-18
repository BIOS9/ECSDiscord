using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using ECSDiscord.Services.Translations;
using ECSDiscord.Util;

namespace ECSDiscord.Services.SlashCommands.Commands;

public class MyCoursesCommand : ISlashCommand
{
    private readonly EnrollmentsService _enrollmentsService;
    private readonly ITranslator _translator;

    public MyCoursesCommand(EnrollmentsService enrollmentsService, ITranslator translator)
    {
        _enrollmentsService = enrollmentsService ?? throw new ArgumentNullException(nameof(enrollmentsService));
        _translator = translator ?? throw new ArgumentNullException(nameof(translator));
    }

    public string Name => "mycourses";

    public SlashCommandProperties Build()
    {
        return new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("List your course channels.")
            .Build();
    }

    public async Task ExecuteAsync(ISlashCommandInteraction command)
    {
        var courses = await _enrollmentsService.GetUserCourses(command.User);
        if (courses.Count == 0)
            await command.RespondAsync($"You are not in any courses. Use `/listcourses` to view a list of all courses.", ephemeral: true);
        else
            await command.RespondAsync("You are in the following courses:\n```" +
                                       courses
                                           .Select(x => $"{x}")
                                           .Aggregate((x, y) => $"{x}\t{y}") +
                                       $"``\n\nUse `/listcourses` to view a list of all courses.", ephemeral: true);
    }
}