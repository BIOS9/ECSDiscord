﻿using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Collections.Generic;
using System.Linq;

namespace ECSDiscord.Util
{
    public static class DiscordUtil
    {
        /// <summary>
        /// Ensure command is only executed in allowed channels.
        /// </summary>
        public static bool CheckConfigChannel(this SocketCommandContext context, string category, IConfigurationRoot config)
        {
            if (context.IsPrivate || (context.User as IGuildUser).GuildPermissions.Administrator) // Allow administrators to use any command in any channel
                return true;

            IConfigurationSection section = config.GetSection("forcedChannels").GetSection(category); // Get command category from config

            List<ulong> allowedChannels = new List<ulong>(); // List of allowed channels to show user
            foreach (IConfigurationSection child in section.GetChildren())
            {
                if (string.IsNullOrWhiteSpace(child.Value)) // Ignore null or empty channels
                    continue;

                // Attempt to convert configured channel to a ulong
                ulong allowedChannel;
                if (!ulong.TryParse(child.Value, out allowedChannel))
                {
                    Log.Error("Invalid forced channel configuration. Expected unsigned long integer or null, found \"{channel}\" in section {section}", 
                        child.Value, category);
                }

                allowedChannels.Add(allowedChannel);

                if (context.Channel.Id == allowedChannel) // Check if channels match
                    return true;
            }

            if (allowedChannels.Count == 0) // If no channels are allowed, assume unconfigured so allow all.
                return true;

            // Log and send message to user.
            context.Channel.SendMessageAsync("Sorry, that command can only be used in " +
                allowedChannels.Select(x => MentionUtils.MentionChannel(x)).Aggregate((x, y) => $"{x}, {y}"));
            Log.Information("User {user} was disallowed access to command because execution is not allowed in channel {channel}",
                context.User.ToString(), context.Channel.Name);

            // Disallow access.
            return false;     
        }

        /// <summary>
        /// Prevent @mentions from pinging.
        /// </summary>
        /// <remarks>
        /// Helps prevent the bot being used to abuse @mentions
        /// </remarks>
        public static string SanitizeMentions(this string message)
        {
            return message.Replace("@", "＠"); // Replace @ mentions with weird alternate @ symbol that doesnt trigger mention
        }
    }
}
