using Discord.Commands;
using System.Threading.Tasks;

namespace DiscordBot.Commands.CommandModules
{
    [Name("CMD_VERIFICATION_NAME")]
    public class VerificationModule : ModuleBase<SocketCommandContext>
    {
        [Command("verify")]
        [Summary("CMD_VERIFY_SUMMARY")]
        [Remarks("CMD_VERIFY_REMARKS")]
        public async Task VerifyAsync()
        {
            await ReplyAsync("Test");
        }
    }
}
