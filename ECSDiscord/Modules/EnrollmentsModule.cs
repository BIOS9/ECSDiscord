using Discord.Commands;
using ECSDiscord.Modules.Util;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace ECSDiscord.BotModules
{
    [Name("Enrollments")]
    public class EnrollmentsModule : ModuleBase<SocketCommandContext>
    {
        private readonly Discord.Commands.CommandService _service;
        private readonly IConfigurationRoot _config;

        public EnrollmentsModule(Discord.Commands.CommandService service, IConfigurationRoot config)
        {
            _service = service;
            _config = config;
        }

        [Command("join")]
        [Alias("enroll", "enrol")]
        [RequireContext(ContextType.Guild)]
        [Summary("Join a uni course channel.")]
        public async Task JoinAsync(params string[] courses)
        {
            if (!Context.CheckConfigChannel("enrollments", _config)) return; // Ensure command is only executed in allowed channels
        }

        [Command("leave")]
        [Alias("unenroll", "unenrol", "disenroll", "disenrol")]
        [RequireContext(ContextType.Guild)]
        [Summary("Leave a uni course channel.")]
        public async Task LeaveAsync(params string[] courses)
        {
            if (!Context.CheckConfigChannel("enrollments", _config)) return; // Ensure command is only executed in allowed channels
        }

        [Command("toggle")]
        [Alias("course", "paper", "disenroll", "disenrol")]
        [RequireContext(ContextType.Guild)]
        [Summary("Join or leave a uni course channel.")]
        public async Task ToggleAsync(params string[] courses)
        {
            if (!Context.CheckConfigChannel("enrollments", _config)) return; // Ensure command is only executed in allowed channels
        }

        [Command("courses")]
        [Alias("list")]
        [RequireContext(ContextType.Guild)]
        public async Task CoursesAsync()
        {
            if (!Context.CheckConfigChannel("enrollments", _config)) return; // Ensure command is only executed in allowed channels
            //ReplyAsync
        }
    }
}
