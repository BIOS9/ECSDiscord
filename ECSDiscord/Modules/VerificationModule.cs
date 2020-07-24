using Discord.Commands;
using ECSDiscord.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static ECSDiscord.Services.VerificationService;

namespace ECSDiscord.Modules
{
    [Name("Verification")]
    public class VerificationModule : ModuleBase<SocketCommandContext>
    {
        private VerificationService _verification;

        public VerificationModule(VerificationService verification)
        {
            _verification = verification;
        }


        [Command("verify")]
        [Summary("Connect your uni username with your ECS discord account.")]
        [Remarks("Supply your uni email address to verify.")]
        public async Task VerifyAsync(string email)
        {
            await ReplyAsync("Processing...");
            
            StringBuilder stringBuilder = new StringBuilder();
            EmailResult result = await _verification.StartVerificationAsync(email, Context.User);
            switch (result)
            {
                case EmailResult.InvalidEmail:
                    stringBuilder.Append($":warning:  Invalid email address. Please use a uni email address.\n");
                    break;
                default:
                case EmailResult.Failure:
                    stringBuilder.Append($":fire:  A server error occured. Please ask and admin to check the logs.\n");
                    break;
                case EmailResult.Success:
                    stringBuilder.Append($":white_check_mark:  Verification email sent!\nPlease check your email for further instructions.\n");
                    break;
            }
        }
    }
}
