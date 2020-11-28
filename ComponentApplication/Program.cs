using Autofac;
using ComponentApplication.Components;
using ComponentApplication.Components.Resources;
using ComponentApplication.Components.Services;
using System;
using System.Collections.Generic;
using System.Linq;
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

            IResourceLoader globalResourceLoader = new PluginResourceLoader("GlobalResources", "Global", true); // Use plugin loader to load global resources.
            AppDomain.CurrentDomain.AssemblyResolve += globalResourceLoader.AssemblyResolve; // Provide assemblies from loaded resources.

            var container = ContainerConfig.Configure(componentLoader.LoadAssemblies());  // Register dependencies.
            using (var scope = container.BeginLifetimeScope()) // Dependency scope for app.
            {
                var serviceManager = scope.Resolve<IServiceManager>(); // Get service manager.
                var services = scope.Resolve<IEnumerable<IService>>(); // Get all loaded services.
                
                services.ToList().ForEach(serviceManager.RegisterService); // Register services in service manager.
                await serviceManager.StartServices(); // Start all services and wait for finish
                await serviceManager.Wait();
            }
        }
    }
}
