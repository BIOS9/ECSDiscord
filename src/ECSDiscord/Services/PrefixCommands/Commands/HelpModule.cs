using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using ECSDiscord.Util;
using Microsoft.Extensions.Configuration;

namespace ECSDiscord.Services.PrefixCommands.Commands;

[Name("Help")]
public class HelpModule : ModuleBase<SocketCommandContext>
{
    private readonly IConfiguration _config;
    private readonly CommandService _service;

    public HelpModule(CommandService service, IConfiguration config)
    {
        _service = service;
        _config = config;
    }

    [Command("help")]
    public async Task HelpAsync()
    {
        var prefix = _config["prefix"];
        var builder = new EmbedBuilder
        {
            Color = new Color(15, 87, 55),
            Description = "These are the commands you can use"
        };

        foreach (var module in _service.Modules)
        {
            string description = null;
            var printedCommands = new HashSet<string>();
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
                builder.AddField(x =>
                {
                    x.Name = module.Name;
                    x.Value = description;
                    x.IsInline = false;
                });
        }

        await ReplyAsync("", false, builder.Build());
    }

    [Command("help")]
    public async Task HelpAsync(string command)
    {
        var result = _service.Search(Context, command);

        if (!result.IsSuccess)
        {
            await ReplyAsync($"Sorry, I couldn't find a command like **{command.SanitizeMentions()}**.");
            return;
        }

        var prefix = _config["prefix"];
        var builder = new EmbedBuilder
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