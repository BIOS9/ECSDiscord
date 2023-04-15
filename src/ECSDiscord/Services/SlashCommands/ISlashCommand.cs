using Discord;
using System.Threading.Tasks;

namespace ECSDiscord.Services.SlashCommands;

public interface ISlashCommand
{
    string Name { get; }
    SlashCommandProperties Build();
    Task ExecuteAsync(ISlashCommandInteraction command);
}
