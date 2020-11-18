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
    class Program
    {
        static async Task Main(string[] args)
        {
            IComponentLoader componentLoader = new PluginComponentLoader();
            var container = ContainerConfig.Configure(componentLoader, componentLoader.LoadAssemblies());
            using (var scope = container.BeginLifetimeScope())
            {
                var components = container.Resolve<IEnumerable<IService>>();
                var serviceManager = container.Resolve<IServiceManager>();
                components.ToList().ForEach(serviceManager.RegisterService);
                await serviceManager.StartServices();
                await serviceManager.StopServices();
            }
        }
    }
}
