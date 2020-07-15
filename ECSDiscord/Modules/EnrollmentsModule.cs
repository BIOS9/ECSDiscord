using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ECSDiscord.BotModules
{
    public class EnrollmentsModule : ModuleBase<SocketCommandContext>
    {
        [Command("join")]
        [Alias("enroll", "enrol")]
        [RequireContext(ContextType.Guild)]
        public async Task JoinAsync()
        {

        }

        [Command("leave")]
        [Alias("unenroll", "unenrol", "disenroll", "disenrol")]
        [RequireContext(ContextType.Guild)]
        public async Task LeaveAsync()
        {

        }

        [Command("toggle")]
        [Alias("course", "paper", "disenroll", "disenrol")]
        [RequireContext(ContextType.Guild)]
        public async Task ToggleAsync()
        {

        }

        [Command("courses")]
        [Alias("list")]
        [RequireContext(ContextType.Guild)]
        public async Task CoursesAsync()
        {
            //ReplyAsync
        }
    }
}
