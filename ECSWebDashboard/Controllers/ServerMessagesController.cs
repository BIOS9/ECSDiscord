using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Discord.WebSocket;
using ECSDiscord.Services;
using ECSWebDashboard.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ECSWebDashboard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize("admin")]
    public class ServerMessagesController : ControllerBase
    {
        private readonly DiscordSocketClient _discord;
        private readonly ServerMessageService _messageService;
        private readonly IConfigurationRoot _config;
        private ulong _guildId;

        public ServerMessagesController(DiscordSocketClient discord, ServerMessageService messageService, IConfigurationRoot config)
        {
            _discord = discord;
            _config = config;
            _messageService = messageService;
            loadConfig();
        }

        // GET: api/ServerMessages
        [HttpGet]
        public async Task<IEnumerable<ServerMessage>> Get()
        {
            return (await _messageService.GetAllMessagesAsync()).Select(x => new ServerMessage(x));
        }

        // GET: api/ServerMessages/5
        [HttpGet("{id}", Name = "Get")]
        public async Task<ServerMessage> Get(ulong id)
        {
            return new ServerMessage(await _messageService.GetMessageAsync(id));
        }

        // POST: api/ServerMessages
        [HttpPost]
        public async Task<ServerMessage> Post([FromBody]CreateServerMessageParams args)
        {
            Discord.IUser discordUser = _discord.GetUser(ulong.Parse(User.FindFirst("discord:id").Value));
            if (discordUser == null)
                throw new ArgumentException("Discord user not found.");

            SocketTextChannel channel = _discord
                .GetGuild(_guildId)
                .GetTextChannel(ulong.Parse(args.ChannelID));
            if (channel == null)
                throw new ArgumentException("Discord channel not found.");

            if (string.IsNullOrEmpty(args.Name))
                throw new ArgumentException("Name cannot be null.");

            if (string.IsNullOrWhiteSpace(args.Content))
                throw new ArgumentException("Content cannot be null.");

            return new ServerMessage(await _messageService.CreateMessageAsync(args.Content, args.Name, discordUser, channel));
        }

        // PUT: api/ServerMessages/5
        [HttpPut("{id}")]
        public async Task<ServerMessage> Put(string id, [FromBody]EditServerMessageParams args)
        {
            Discord.IUser discordUser = _discord.GetUser(ulong.Parse(User.FindFirst("discord:id").Value));
            if (discordUser == null)
                throw new ArgumentException("Discord user not found.");

            if (string.IsNullOrEmpty(args.Name))
                throw new ArgumentException("Name cannot be null.");

            if (string.IsNullOrWhiteSpace(args.Content))
                throw new ArgumentException("Content cannot be null.");

            return new ServerMessage(await _messageService.EditMessageAsync(ulong.Parse(id), args.Content, args.Name, discordUser));
        }

        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public async Task Delete(string id)
        {
            await _messageService.DeleteMessageAsync(ulong.Parse(id));
        }

        private void loadConfig()
        {
            _guildId = ulong.Parse(_config["guildId"]);
        }
    }
}
