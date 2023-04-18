using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using ECSDiscord.Services.Translations;
using ECSDiscord.Util;

namespace ECSDiscord.Services.SlashCommands.Commands;

public class LeaveAllCommand : ISlashCommand
{
    private readonly EnrollmentsService _enrollmentsService;
    private readonly ITranslator _translator;

    public LeaveAllCommand(EnrollmentsService enrollmentsService, ITranslator translator)
    {
        _enrollmentsService = enrollmentsService ?? throw new ArgumentNullException(nameof(enrollmentsService));
        _translator = translator ?? throw new ArgumentNullException(nameof(translator));
    }

    public string Name => "leaveall";

    public SlashCommandProperties Build()
    {
        return new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Leave all course channels.")
            .Build();
    }

    public async Task ExecuteAsync(ISlashCommandInteraction command)
    {
        var courses = await _enrollmentsService.GetUserCourses(command.User);
        if (courses.Count == 0)
        {
            await command.RespondAsync(_translator.T("ENROLLMENT_NO_COURSES_JOINED"), ephemeral: true);
            return;
        }

        await command.DeferAsync(ephemeral: true);
        // Add user to courses
        var stringBuilder = new StringBuilder();
        foreach (var course in courses)
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