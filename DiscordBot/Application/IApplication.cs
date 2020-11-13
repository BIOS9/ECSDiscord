using System.Threading.Tasks;

namespace DiscordBot.Application
{
    public interface IApplication
    {
        Task RunAsync();
    }
}