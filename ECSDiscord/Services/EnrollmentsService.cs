using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ECSDiscord.Services
{
    public class EnrollmentsService
    {
        private readonly DiscordSocketClient _discord;
        private readonly IConfigurationRoot _config;
        
        public enum EnrollmentResult
        {
            Success,
            CourseNotExist,
            AlreadyJoined,
            AlreadyLeft,
        }

        public EnrollmentsService(DiscordSocketClient discord, IConfigurationRoot config)
        {
            _discord = discord;
            _config = config;
        }

        public async Task<EnrollmentResult> EnrollUser(string course, SocketGuildUser user)
        {

        }

        public async Task<EnrollmentResult> DisenrollUser(string course, SocketGuildUser user)
        {

        }

        public async Task<List<string>> DisenrollUser(string course, SocketGuildUser user)
        {

        }
    }
}
