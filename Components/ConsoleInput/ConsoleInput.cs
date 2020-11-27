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
        public ServiceState State { get; private set; }

        private readonly IServiceManager _serviceManager;

        public ConsoleInput(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            State = ServiceState.Running;
            await Task.Factory.StartNew(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                    processCommand(Console.ReadLine());
            }, cancellationToken);
        }

        private void processCommand(string command)
        {
            if (command.Equals("stop", StringComparison.OrdinalIgnoreCase))
                _serviceManager.StopServices();
        }

        public Task StopAsync()
        {
            State = ServiceState.Stopped;
            return Task.CompletedTask;
        }
    }
}
