using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ECSDiscord.Services;
using ECSDiscord.Util;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
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

        

        
    }
}
