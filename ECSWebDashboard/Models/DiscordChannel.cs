using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECSWebDashboard.Models
{
    public class DiscordChannel
    {
        public string ID { get; set; }
        public string Name { get; set; }

        public DiscordChannel() { }

        public DiscordChannel(Discord.IChannel channel)
        {
            ID = channel?.Id.ToString() ?? string.Empty;
            Name = channel?.Name ?? string.Empty;
        }
    }
}
