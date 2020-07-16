using Discord;
using Discord.Commands;
using ECSDiscord.Modules.Util;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECSDiscord.Modules
{
    [Name("Help")]
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        private readonly Discord.Commands.CommandService _service;
        private readonly IConfigurationRoot _config;

        public HelpModule(Discord.Commands.CommandService service, IConfigurationRoot config)
        {
            _service = service;
            _config = config;
        }

        [Command("help")]
        public async Task HelpAsync()
        {
            if (!Context.CheckConfigChannel("help", _config)) return; // Ensure command is only executed in allowed channels

            string prefix = _config["prefix"];
            var builder = new EmbedBuilder()
            {
                Color = new Color(15, 87, 55),
                Description = "These are the commands you can use"
            };

            foreach (var module in _service.Modules)
            {
                string description = null;
                HashSet<string> printedCommands = new HashSet<string>();
                foreach (var cmd in module.Commands)
                {
                    var result = await cmd.CheckPreconditionsAsync(Context);
                    if (result.IsSuccess)
                    {
                        var cmdStr = $"{prefix}{cmd.Aliases.First()}\n";
                        if (printedCommands.Contains(cmdStr))
                            continue;
                        printedCommands.Add(cmdStr);
                        description += cmdStr;
                    }
                }

                if (!string.IsNullOrWhiteSpace(description))
                {
                    builder.AddField(x =>
                    {
                        x.Name = module.Name;
                        x.Value = description;
                        x.IsInline = false;
                    });
                }
            }

            await ReplyAsync("", false, builder.Build());
        }

        [Command("help")]
        public async Task HelpAsync(string command)
        {
            if (!Context.CheckConfigChannel("help", _config)) return; // Ensure command is only executed in allowed channels

            var result = _service.Search(Context, command);

            if (!result.IsSuccess)
            {
                await ReplyAsync($"Sorry, I couldn't find a command like **{command.SanitizeMentions()}**.");
                return;
            }

            string prefix = _config["prefix"];
            var builder = new EmbedBuilder()
            {
                Color = new Color(15, 87, 55),
                Description = $"Here are some commands like **{command.SanitizeMentions()}**"
            };

            foreach (var match in result.Commands)
            {
                var cmd = match.Command;

                builder.AddField(x =>
                {
                    x.Name = string.Join(", ", cmd.Aliases);
                    x.Value = $"Parameters: {string.Join(", ", cmd.Parameters.Select(p => p.Name))}\n" +
                              $"Summary: {cmd.Summary}";
                    x.IsInline = false;
                });
            }

            await ReplyAsync("", false, builder.Build());
        }
    }
}
