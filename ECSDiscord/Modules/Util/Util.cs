using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ECSDiscord.Modules.Util
{
    public static class Util
    {
        /// <summary>
        /// Ensure command is only executed in allowed channels.
        /// </summary>
        public static bool CheckConfigChannel(this SocketCommandContext context, string category, IConfigurationRoot config)
        {
            IConfigurationSection section = config.GetSection("forcedChannels").GetSection(category);

            List<ulong> allowedChannels = new List<ulong>();
            foreach (IConfigurationSection child in section.GetChildren())
            {
                if (string.IsNullOrWhiteSpace(child.Value))
                    continue;

                ulong allowedChannel;
                if (!ulong.TryParse(child.Value, out allowedChannel))
                {
                    Log.Error("Invalid forced channel configuration. Expected unsigned long integer or null, found \"{channel}\" in section {section}", 
                        child.Value, category);
                }

                allowedChannels.Add(allowedChannel);

                if (context.Channel.Id == allowedChannel)
                    return true;
            }

            if (allowedChannels.Count == 0)
                return true;

            context.Channel.SendMessageAsync("Sorry, that command can only be used in " +
                allowedChannels.Select(x => MentionUtils.MentionChannel(x)).Aggregate((x, y) => $"{x}, {y}"));
            Log.Information("User {user} was disallowed access to command because execution is not allowed in channel {channel}",
                context.User.ToString(), context.Channel.Name);
            return false;     
        }
    }
}
