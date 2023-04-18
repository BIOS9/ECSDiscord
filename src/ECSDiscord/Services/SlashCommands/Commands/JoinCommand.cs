using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using ECSDiscord.Services.Translations;
using ECSDiscord.Util;

namespace ECSDiscord.Services.SlashCommands.Commands;

public class JoinCommand : ISlashCommand
{
    private readonly EnrollmentsService _enrollmentsService;
    private readonly ITranslator _translator;

    public JoinCommand(EnrollmentsService enrollmentsService, ITranslator translator)
    {
        _enrollmentsService = enrollmentsService ?? throw new ArgumentNullException(nameof(enrollmentsService));
        _translator = translator ?? throw new ArgumentNullException(nameof(translator));
    }

    public string Name => "join";

    public SlashCommandProperties Build()
    {
        return new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Join one or more course channels.")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("courses")
                .WithDescription("Space-separated list of course channels you want to join. e.g. comp102 engr101 cybr171")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.String))
            .Build();
    }

    public async Task ExecuteAsync(ISlashCommandInteraction command)
    {
        // Ensure course list is valid
        string[] courses = ((string)command.Data.Options.First().Value).Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
        if (!_enrollmentsService.CheckCourseString(courses, true, out var errorMessage, out var formattedCourses))
        {
            await command.RespondAsync(errorMessage, ephemeral: true);
            return;
        }

        await command.DeferAsync(ephemeral: true);
        if (await _enrollmentsService.RequiresVerification(command.User))
        {
            await command.FollowupAsync(_translator.T("ENROLLMENT_VERIFICATION_REQUIRED_ANY"), ephemeral: true);
            return;
        }

        var userCourses = await _enrollmentsService.GetUserCourses(command.User);
        var courseCount = userCourses.Count;
        const int maxCourses = 15;

        if (courseCount >= maxCourses)
        {
            await command.FollowupAsync(_translator.T("ENROLLMENT_MAX_COURSE_COUNT"), ephemeral: true);
            return;
        }

        // Add user to courses
        var stringBuilder = new StringBuilder();
        foreach (var course in formattedCourses)
        {
            if (course.Equals("boomer", StringComparison.OrdinalIgnoreCase))
            {
                stringBuilder.Append(_translator.T("ENROLLMENT_OK_BOOMER"));
                continue;
            }

            var result = await _enrollmentsService.EnrollUser(course, command.User);
            switch (result)
            {
                case EnrollmentsService.EnrollmentResult.AlreadyJoined:
                    stringBuilder.Append(_translator.T("ENROLLMENT_ALREADY_ENROLLED", course));
                    break;
                case EnrollmentsService.EnrollmentResult.CourseNotExist:
                    stringBuilder.Append(_translator.T("ENROLLMENT_INVALID_COURSE", course));
                    break;
                case EnrollmentsService.EnrollmentResult.Blacklisted:
                    await command.FollowupAsync(_translator.T("ENROLLMENT_BLACKLISTED"), ephemeral: true);
                    return;
                default:
                case EnrollmentsService.EnrollmentResult.Failure:
                    stringBuilder.Append(_translator.T("ENROLLMENT_SERVER_ERROR", course));
                    break;
                case EnrollmentsService.EnrollmentResult.Success:
                    ++courseCount;
                    stringBuilder.Append(_translator.T("ENROLLMENT_JOIN_SUCCESS", course));
                    break;
                case EnrollmentsService.EnrollmentResult.Unverified:
                    stringBuilder.Append(_translator.T("ENROLLMENT_VERIFICATION_REQUIRED", course));
                    break;
            }

            if (courseCount >=
                maxCourses) // This one is here to allow joined courses to be printed out even if the max is reached.
            {
                await command.FollowupAsync(_translator.T("ENROLLMENT_MAX_COURSE_COUNT"), ephemeral: true);
                break;
            }
        }

        await command.FollowupAsync(stringBuilder.ToString().Trim(), ephemeral: true);
    }
}