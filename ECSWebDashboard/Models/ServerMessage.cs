using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECSDiscord.Services;

namespace ECSWebDashboard.Models
{
    public class ServerMessage
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public DiscordChannel Channel { get; set; }
        public long CreatedAt { get; set; }
        public DiscordUser Creator { get; set; }
        public long EditedAt { get; set; }
        public DiscordUser Editor { get; set; }
        public string Content { get; set; }

        public ServerMessage() { }

        public ServerMessage(ServerMessageService.ServerMessage serverMessage)
        {
            ID = serverMessage.Message.Id.ToString();
            CreatedAt = serverMessage.CreatedAt.ToUnixTimeSeconds();
            Creator = new DiscordUser(serverMessage.Creator);
            EditedAt = serverMessage.EditedAt.ToUnixTimeSeconds();
            Editor = new DiscordUser(serverMessage.Editor);
            Content = serverMessage.Content;
            Name = serverMessage.Name;
            Channel = new DiscordChannel(serverMessage.Message.Channel);
        }
    }
}
