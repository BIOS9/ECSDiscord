using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using ECSWebDashboard.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ECSWebDashboard.Controllers
{
    [Route("api/Server")]
    [ApiController]
    [Authorize("admin")]
    public class GeneralServerController : Controller
    {
        private readonly DiscordSocketClient _discord;
        private readonly IConfigurationRoot _config;
        private ulong _guildId;

        public GeneralServerController(DiscordSocketClient discord, IConfigurationRoot config)
        {
            _discord = discord;
            _config = config;
            loadConfig();
        }

        // GET: api/text-channels
        [HttpGet("text-channels")]
        public IEnumerable<DiscordChannel> GetChannels()
        {
            return _discord.GetGuild(_guildId).TextChannels.Select(x => new DiscordChannel(x));
        }
        
        // GET: api/text-channels
        [HttpGet("users")]
        public IEnumerable<DiscordUser> GetUsers()
        {
            return _discord.GetGuild(_guildId).Users.Select(x => new DiscordUser(x));
        }

        private void loadConfig()
        {
            _guildId = ulong.Parse(_config["guildId"]);
        }
    }
}
