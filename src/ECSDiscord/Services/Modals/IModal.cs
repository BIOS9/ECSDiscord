using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace ECSDiscord.Services.Modals;

public interface IModal
{
    string Name { get; }
    string CustomId { get; set; }
    Task<Modal> BuildAsync(string customId);
    Task ExecuteAsync(SocketModal modalInteraction);
}