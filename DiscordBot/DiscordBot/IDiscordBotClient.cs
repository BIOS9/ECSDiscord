using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.DiscordBot
{
    public interface IDiscordBotClient : IDiscordClient
    {
        event Func<LogMessage, Task> Log;
        Task LoginAsync(TokenType tokenType, string token, bool validateToken = true);
        Task LogoutAsync();
    }
}
