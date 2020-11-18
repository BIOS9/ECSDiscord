using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ComponentApplication.Components.Services
{
    internal class ServiceManager : IServiceManager
    {
        private ISet<IService> _services = new HashSet<IService>();

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
            List<Task> tasks = new List<Task>();
            foreach (IService service in _services)
            {
                Console.WriteLine($"Starting service: {service.Name} Version {service.Version}");
                tasks.Add(service.StartAsync());
            }
            return Task.WhenAll(tasks);
        }

        public Task StopServices()
        {
            List<Task> tasks = new List<Task>();
            foreach (IService service in _services)
            {
                Console.WriteLine($"Stopping service: {service.Name} Version {service.Version}");
                tasks.Add(service.StopAsync());
            }
            return Task.WhenAll(tasks);
        }
    }
}
