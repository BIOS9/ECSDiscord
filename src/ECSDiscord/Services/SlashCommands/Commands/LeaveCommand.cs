using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using ECSDiscord.Services.Translations;
using ECSDiscord.Util;

namespace ECSDiscord.Services.SlashCommands.Commands;

public class LeaveCommand : ISlashCommand
{
    private readonly EnrollmentsService _enrollmentsService;
    private readonly ITranslator _translator;

    public LeaveCommand(EnrollmentsService enrollmentsService, ITranslator translator)
    {
        _enrollmentsService = enrollmentsService ?? throw new ArgumentNullException(nameof(enrollmentsService));
        _translator = translator ?? throw new ArgumentNullException(nameof(translator));
    }

    public string Name => "leave";

    public SlashCommandProperties Build()
    {
        return new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Leave one or more course channels.")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("courses")
                .WithDescription("The space-separated list of course channels you want to leave. e.g. comp102 engr101 cybr171")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.String))
            .Build();
    }

    public async Task ExecuteAsync(ISlashCommandInteraction command)
    {
        // Ensure course list is valid
        string[] courses = ((string)command.Data.Options.First().Value).Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);

        // Ensure course list is valid
        if (!_enrollmentsService.CheckCourseString(courses, true, out var errorMessage, out var formattedCourses))
        {
            await command.RespondAsync(errorMessage, ephemeral: true);
            return;
        }

        await command.DeferAsync(ephemeral: true);

        // Add user to courses
        var stringBuilder = new StringBuilder();
        foreach (var course in formattedCourses)
        {
            var result = await _enrollmentsService.DisenrollUser(course, command.User);
            switch (result)
            {
                case EnrollmentsService.EnrollmentResult.AlreadyLeft:
                    stringBuilder.Append(_translator.T("ENROLLMENT_ALREADY_LEFT", course));
                    break;
                case EnrollmentsService.EnrollmentResult.CourseNotExist:
                    stringBuilder.Append(_translator.T("ENROLLMENT_INVALID_COURSE", course));
                    break;
                default:
                case EnrollmentsService.EnrollmentResult.Failure:
                    stringBuilder.Append(_translator.T("ENROLLMENT_SERVER_ERROR", course));
                    break;
                case EnrollmentsService.EnrollmentResult.Success:
                    stringBuilder.Append(_translator.T("ENROLLMENT_LEAVE_SUCCESS", course));
                    break;
            }
        }

        await command.FollowupAsync(stringBuilder.ToString().Trim(), ephemeral: true);
    }
}