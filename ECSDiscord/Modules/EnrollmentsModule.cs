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
        public async Task JoinAsync()
        {

        }

        [Command("leave")]
        [Alias("unenroll", "unenrol", "disenroll", "disenrol")]
        public async Task LeaveAsync()
        {

        }

        [Command("toggle")]
        [Alias("course", "paper", "disenroll", "disenrol")]
        public async Task ToggleAsync()
        {

        }

        [Command("courses")]
        [Alias("list")]
        public async Task CoursesAsync()
        {

        }
    }
}
