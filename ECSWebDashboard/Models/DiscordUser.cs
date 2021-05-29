using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECSWebDashboard.Models
{
    public class DiscordUser
    {
        public string ID { get; set; }
        public string Username { get; set; }
        public string Avatar { get; set; }

        public DiscordUser() { }

        public DiscordUser(Discord.IUser user)
        {
            ID = user.Id.ToString();
            Username = user.Username;
            Avatar = user.GetAvatarUrl(Discord.ImageFormat.Auto);
        }
    }
}
