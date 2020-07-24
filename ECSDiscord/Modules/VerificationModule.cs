using Discord.Commands;
using ECSDiscord.Services;
using ECSDiscord.Util;
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
            try
            {
                await Context.Message.DeleteAsync();
            }
            catch { }

            StringBuilder stringBuilder = new StringBuilder();

            if (IsValidCode(email))
            {
                VerificationResult result = await _verification.FinishVerificationAsync(email, Context.User);
                switch (result)
                {
                    case VerificationResult.InvalidToken:
                        stringBuilder.Append($":warning:  Invalid verification code.\n");
                        break;
                    default:
                    case VerificationResult.Failure:
                        stringBuilder.Append($":fire:  A server error occured. Please ask an admin to check the logs.\n");
                        break;
                    case VerificationResult.Success:
                        stringBuilder.Append($":white_check_mark:  You are now verified!\n");
                        break;
                }
            }
            else
            {
                EmailResult result = await _verification.StartVerificationAsync(email, Context.User);
                switch (result)
                {
                    case EmailResult.InvalidEmail:
                        stringBuilder.Append($":warning:  Invalid email address. Please use a uni email address.\n");
                        break;
                    default:
                    case EmailResult.Failure:
                        stringBuilder.Append($":fire:  A server error occured. Please ask an admin to check the logs.\n");
                        break;
                    case EmailResult.Success:
                        stringBuilder.Append($":white_check_mark:  Verification email sent!\nPlease check your email for further instructions.\n");
                        break;
                }
            }
            await ReplyAsync(stringBuilder.ToString().Trim().SanitizeMentions());
        }
    }
}
