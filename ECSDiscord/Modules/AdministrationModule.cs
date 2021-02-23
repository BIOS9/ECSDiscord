using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ECSDiscord.Services;
using ECSDiscord.Util;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ECSDiscord.Modules
{
    [Name("Administration")]
    public class AdministrationModule : ModuleBase<SocketCommandContext>
    {
        private readonly Discord.Commands.CommandService _service;
        private readonly StorageService _storage;
        private readonly CourseService _courses;
        private readonly VerificationService _verification;
        private readonly IConfigurationRoot _config;

        public AdministrationModule(Discord.Commands.CommandService service, IConfigurationRoot config, CourseService courses, VerificationService verification, StorageService storage)
        {
            _storage = storage;
            _service = service;
            _config = config;
            _courses = courses;
            _verification = verification;
        }

        [Command("downloaddata")]
        [Alias("downloadmydata", "downloaduserdata")]
        [Summary("Downloads a copy of your data that we hold.")]
        public async Task DownloadData()
        {
            try
            {
                await ReplyAsync("Processing...");
                if(!Context.IsPrivate)
                {
                    await ReplyAsync(":warning:  This command must be sent via **Direct Messages**");
                    return;
                }
                ulong user = Context.User.Id;
                string userData = await _storage.GetUserDataAsync(user);
                byte[] dataBytes = Encoding.UTF8.GetBytes(userData);
                using (MemoryStream ms = new MemoryStream())
                {
                    await ms.WriteAsync(dataBytes);
                    ms.Seek(0, SeekOrigin.Begin);
                    await Context.Channel.SendFileAsync(ms, "userdata.json");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to download user data {message}", ex.Message);
                await ReplyAsync($"Failed to fetch user data due to an error.");
            }
        }
    }
}
