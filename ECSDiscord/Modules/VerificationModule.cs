using Discord.Commands;
using ECSDiscord.Services;
using ECSDiscord.Util;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfigurationRoot _config;
        private VerificationService _verification;

        public VerificationModule(VerificationService verification, IConfigurationRoot config)
        {
            _verification = verification;
            _config = config;
        }

        [Command("verify")]
        [Summary("Connect your uni username with your ECS discord account.")]
        [Remarks("Supply your uni email address to verify.")]
        public async Task VerifyAsync()
        {
            await ReplyAsync($":warning:  Invalid email address.\n" +
                $"Please provide your uni email address e.g.\n" +
                $"```{_config["prefix"]}verify username@myvuw.ac.nz```");
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
                    case VerificationResult.TokenExpired:
                        stringBuilder.Append($":clock1:  That token has expired! Please verify again.\n");
                        break;
                    case VerificationResult.NotInServer:
                        stringBuilder.Append($":warning:  You are not in the Discord server!\n");
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
                        stringBuilder.Append($":white_check_mark:  Verification email sent!\nPlease check your email for further instructions.\n\nIf you do not receive the email **PLEASE CHECK YOUR SPAM**");
                        break;
                }
            }
            await ReplyAsync(stringBuilder.ToString().Trim().SanitizeMentions());
        }
    }
}
