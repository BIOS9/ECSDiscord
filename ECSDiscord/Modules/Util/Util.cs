using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECSDiscord.Modules.Util
{
    public static class Util
    {
        public static bool CheckConfigChannel(this SocketCommandContext context, string category, IConfigurationRoot config)
        {
            string value = config.GetSection("forcedChannels").GetSection(category).Value;
            if (string.IsNullOrWhiteSpace(value))
                return true;

            ulong allowedChannel;
            if (!ulong.TryParse(value, out allowedChannel))
            {
                Log.Error("Invalid forced channel configuration. Expected unsigned long integer or null, found \"{channel}\"", value);
                return true;
            }
            
            if(context.Channel.Id != allowedChannel)
            {
                context.Channel.SendMessageAsync("Sorry, that command can only be used in " + MentionUtils.MentionChannel(allowedChannel));
                Log.Information("User {user} disallowed access to command because execution is not allowed in channel {channel}", 
                    context.User.ToString(), context.Channel.Name);
                return false;
            }

            return true;            
        }
    }
}
