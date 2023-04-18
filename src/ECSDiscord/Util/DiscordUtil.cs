using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace ECSDiscord.Util;

public static class DiscordUtil
{
    /// <summary>
    ///     Prevent @mentions from pinging.
    /// </summary>
    /// <remarks>
    ///     Helps prevent the bot being used to abuse @mentions
    /// </remarks>
    public static string SanitizeMentions(this string message)
    {
        return
            message.Replace("@", "＠"); // Replace @ mentions with weird alternate @ symbol that doesnt trigger mention
    }
}