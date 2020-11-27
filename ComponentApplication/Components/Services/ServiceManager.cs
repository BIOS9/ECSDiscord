using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ComponentApplication.Components.Services
{
    internal class ServiceManager : IServiceManager
    {
        private readonly ISet<IService> _services = new HashSet<IService>();
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
                tasks.Add(service.StartAsync());
            }
            return Task.WhenAll(tasks); // Wait on all services to start.
        }

        public Task StopServices()
        {
            _logger.LogInformation("Stopping services...");
            List<Task> tasks = new List<Task>();
            foreach (IService service in _services) // Stop all registered services.
            {
                _logger.LogInformation("Stopping service: {name} Version {version}", service.Name, service.Version);
                tasks.Add(service.StopAsync());
            }
            return Task.WhenAll(tasks); // Wait on all services to stop.
        }
    }
}
