using System.Threading.Tasks;
using Discord;

namespace ECSDiscord.Services.SlashCommands;

public interface ISlashCommand
{
    string Name { get; }
    SlashCommandProperties Build();
    Task ExecuteAsync(ISlashCommandInteraction command);
}