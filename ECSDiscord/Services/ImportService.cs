using Discord.WebSocket;
using ECSDiscord.Core.Translations;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static ECSDiscord.Services.CourseService;
using static ECSDiscord.Services.EnrollmentsService;

namespace ECSDiscord.Services
{
    public class ImportService
    {
        private readonly DiscordSocketClient _discord;
        private readonly EnrollmentsService _enrollmentsService;
        private readonly CourseService _courseService;
        private readonly ITranslator _translator;

        public ImportService(ITranslator translator, DiscordSocketClient discord, EnrollmentsService enrollmentsService, CourseService courseService)
        {
            Log.Debug("Import service loading.");
            _enrollmentsService = enrollmentsService;
            _courseService = courseService;
            _translator = translator;
            _discord = discord;
            Log.Debug("Import service loaded.");
        }

        public async Task<string> ImportCoursePermissions(SocketGuild guild)
        {
            StringBuilder importLog = new StringBuilder();
            importLog.AppendLine($"Discord permission import log {DateTime.Now.ToString()}");
            IList<Course> courses = await _courseService.GetCourses();
            foreach(Course c in courses)
            {
                try
                {
                    SocketGuildChannel discordChannel = guild.GetChannel(c.DiscordId);
                    foreach (SocketGuildUser user in discordChannel.Users)
                    {
                        //if (user.GuildPermissions.Administrator) // Skip admins
                        //continue;
                        if (user.IsBot) // Skip bots
                            continue;

                        EnrollmentResult result = await _enrollmentsService.EnrollUser(c.Code, user);
                        importLog.AppendLine($"Course: {c.Code}\tChannel:{c.DiscordId}\tUser:{user.Id}\tResult:{result.ToString()}");
                        await Task.Delay(250);
                    }
                }
                catch(Exception ex)
                {
                    importLog.AppendLine($"Course: {c.Code}\tChannel:{c.DiscordId}\tResult:Error {ex.Message}");
                    await Task.Delay(250);
                }
            }
            return importLog.ToString();
        }
    }
}
