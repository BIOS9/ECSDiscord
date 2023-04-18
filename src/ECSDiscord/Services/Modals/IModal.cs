using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace ECSDiscord.Services.Modals;

public interface IModal
{
    string CustomId { get; }
    Task ExecuteAsync(SocketModal modalInteraction);
}