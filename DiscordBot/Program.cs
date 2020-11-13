using Autofac;
using DiscordBot.Application;
using System.Threading.Tasks;

namespace DiscordBot
{
    class Program
    {
        static Task Main(string[] args)
        {
            var container = ContainerConfig.Configure();
            using (var scope = container.BeginLifetimeScope())
            {
                var app = scope.Resolve<IApplication>();
                return app.RunAsync();
            }
        }
    }
}
