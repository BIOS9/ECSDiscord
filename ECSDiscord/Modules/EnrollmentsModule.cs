using Discord.Commands;
using ECSDiscord.Modules.Util;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace ECSDiscord.BotModules
{
    [Name("Enrollments")]
    [RequireContext(ContextType.Guild)]
    public class EnrollmentsModule : ModuleBase<SocketCommandContext>
    {
        private readonly IConfigurationRoot _config;

        public EnrollmentsModule(IConfigurationRoot config)
        {
            _config = config;
        }

        [Command("join")]
        [Alias("enroll", "enrol")]
        [Summary("Join a uni course channel.")]
        public async Task JoinAsync(params string[] courses)
        {
            if (!Context.CheckConfigChannel("enrollments", _config)) return; // Ensure command is only executed in allowed channels
        }

        [Command("leave")]
        [Alias("unenroll", "unenrol", "disenroll", "disenrol")]
        [Summary("Leave a uni course channel.")]
        public async Task LeaveAsync(params string[] courses)
        {
            if (!Context.CheckConfigChannel("enrollments", _config)) return; // Ensure command is only executed in allowed channels
        }

        [Command("togglecourse")]
        [Alias("rank", "role", "course", "paper", "disenroll", "disenrol")]
        [Summary("Join or leave a uni course channel.")]
        public async Task ToggleCourseAsync(params string[] courses)
        {
            if (!Context.CheckConfigChannel("enrollments", _config)) return; // Ensure command is only executed in allowed channels
        }

        [Command("courses")]
        [Alias("list")]
        [Summary("List the courses you are in.")]
        public async Task CoursesAsync()
        {
            if (!Context.CheckConfigChannel("enrollments", _config)) return; // Ensure command is only executed in allowed channels
            //ReplyAsync
        }
    }
}
