using Autofac;
using ComponentApplication.Components;
using ComponentApplication.Components.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ComponentApplication
{
    class Program
    {
        static Task Main(string[] args)
        {
            IComponentLoader componentLoader = new PluginComponentLoader();
            var container = ContainerConfig.Configure(componentLoader.LoadAssemblies());
            using (var scope = container.BeginLifetimeScope())
            {
                var components = container.Resolve<IEnumerable<IComponent>>();

                //components.Select(x => x.LoadAsync());
                foreach (IComponent component in components)
                {
                    Console.WriteLine($"Loading component: {component.Name} Version {component.Version}");
                    component.LoadAsync();
                }
            }
            return Task.CompletedTask;
        }
    }
}
