using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ComponentApplication.Components.Services
{
    internal class ServiceManager : IServiceManager
    {
        private readonly IList<IService> _services = new List<IService>();
        private readonly IDictionary<IService, CancellationTokenSource> _cancellationTokens = new Dictionary<IService, CancellationTokenSource>();
        private readonly ILogger _logger;
        private Task _waitTask;

        public ServiceManager(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger("Service Manager");
        }

        public void RegisterService(IService service)
        {
            _services.Add(service);
        }

        public void DeregisterService(IService service)
        {
            _services.Remove(service);
        }

        public async Task StartServices()
        {
            _logger.LogInformation("Starting services...");
            List<Task> tasks = new List<Task>();
            foreach (IService service in _services) // Start all registered services.
            {
                _logger.LogInformation("Starting service: {name} Version {version}", service.Name, service.Version);
                tasks.Add(service.StartAsync());
            }
            await Task.WhenAll(tasks); // Wait on all services to start.
            _logger.LogInformation("All services started...");
            runServices();
        }

        private void runServices()
        {
            _logger.LogDebug("Running services...");
            List<Task> tasks = new List<Task>();
            foreach (IService service in _services) // Run all registered services.
            {
                _logger.LogDebug("Running service: {name} Version {version}", service.Name, service.Version);
                var tokenSource = new CancellationTokenSource();
                _cancellationTokens.Add(service, tokenSource);
                tasks.Add(service.RunAsync(tokenSource.Token));
            }
            _waitTask = Task.Run(async () =>
            {
                try
                {
                    await Task.WhenAll(tasks); // Wait on all services to finish.
                }
                catch (TaskCanceledException) { } // Suppress cancellation exception.
            });
        }

        public async Task StopServices()
        {
            _logger.LogInformation("Stopping services...");
            List<Task> tasks = new List<Task>();
            foreach (IService service in _services) // Stop all registered services.
            {
                _logger.LogInformation("Stopping service: {name} Version {version}", service.Name, service.Version);
                _cancellationTokens[service]?.Cancel();
                _cancellationTokens[service]?.Dispose();
                _cancellationTokens.Remove(service);
                tasks.Add(service.StopAsync());
            }
            await Task.WhenAll(tasks); // Wait on all services to stop.
        }

        public Task Wait()
        {
            return _waitTask;
        }
    }
}
