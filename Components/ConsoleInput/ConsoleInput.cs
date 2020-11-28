using ComponentApplication.Components.Services;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static ComponentApplication.Components.Services.IService;

namespace ConsoleInput
{
    public class ConsoleInput : IService
    {
        public string Name => "Console Input";
        public Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        private readonly IServiceManager _serviceManager;

        public ConsoleInput(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        public Task StartAsync()
        {
            return Task.CompletedTask;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            await Task.Factory.StartNew(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                    processCommand(Console.ReadLine());
            }, cancellationToken);
        }

        public Task StopAsync()
        {
            return Task.CompletedTask;
        }
        private void processCommand(string command)
        {
            if (command.Equals("stop", StringComparison.OrdinalIgnoreCase))
                _serviceManager.StopServices().Wait();
        }
    }
}
