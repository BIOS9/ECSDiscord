using Discord;
using Discord.Commands;
using ECSDiscord.Core.Translations;
using ECSDiscord.Services;
using ECSDiscord.Util;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECSDiscord.Modules
{
    [Name("Import")]
    public class ImportModule : ModuleBase<SocketCommandContext>
    {
        private readonly Discord.Commands.CommandService _service;
        private readonly IConfiguration _config;
        private readonly ImportService _importService;
        private readonly ITranslator _translator;

        public ImportModule(ITranslator translator, Discord.Commands.CommandService service, ImportService importService, IConfiguration config)
        {
            _service = service;
            _config = config;
            _translator = translator;
            _importService = importService;
        }

        [Command("importpermissions")]
        [Summary("Imports course permissions from the VicBot format by converting role permissions into user permissions.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ImportPermissions()
        {
            await ReplyAsync(_translator.T("IMPORT_PERMISSIONS_START"));
            string log = await _importService.ImportCoursePermissions(Context.Guild);
            using (var stream = new MemoryStream())
            {
                await stream.WriteAsync(Encoding.UTF8.GetBytes(log));
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, "importLog.txt", _translator.T("IMPORT_PERMISSIONS_END"));
            }
        }
    }
}
