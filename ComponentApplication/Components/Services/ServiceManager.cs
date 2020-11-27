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

        public Task StartServices()
        {
            _logger.LogInformation("Starting services...");
            List<Task> tasks = new List<Task>();
            foreach (IService service in _services) // Start all registered services.
            {
                _logger.LogInformation("Starting service: {name} Version {version}", service.Name, service.Version);

                var tokenSource = new CancellationTokenSource();
                _cancellationTokens.Add(service, tokenSource);
                tasks.Add(service.StartAsync(tokenSource.Token));
            }
            return Task.WhenAll(tasks); // Wait on all services to start.
        }

        public void StopServices()
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
            Task.WhenAll(tasks).Wait(); // Wait on all services to stop.
        }
    }
}
