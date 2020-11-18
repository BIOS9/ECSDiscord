using Autofac;
using ComponentApplication.Components;
using ComponentApplication.Components.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ComponentApplication
{
    /// <summary>
    /// Main class for the application.
    /// Handles core setup and execution.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Entry point.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        static async Task Main(string[] args)
        {
            IComponentLoader componentLoader = new PluginComponentLoader(); // Use plugin loader to load components.
            var container = ContainerConfig.Configure(componentLoader, componentLoader.LoadAssemblies());  // Register dependencies.
            using (var scope = container.BeginLifetimeScope()) // Dependency scope for app.
            {
                var services = container.Resolve<IEnumerable<IService>>(); // Get all loaded services.
                var serviceManager = container.Resolve<IServiceManager>(); // Get service manager.
                services.ToList().ForEach(serviceManager.RegisterService); // Register services in service manager.
                await serviceManager.StartServices(); // Start all services and wait for finish.
                await serviceManager.StopServices(); // Stop all services to cleanup.
            }
        }
    }
}
