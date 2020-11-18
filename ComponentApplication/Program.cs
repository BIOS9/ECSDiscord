using Autofac;
using ComponentApplication.Components;
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
                return run(components);
            }
        }

        static async Task run(IEnumerable<IComponent> components)
        {
            List<Task> startTasks = new List<Task>();
            foreach (IComponent component in components)
            {
                Console.WriteLine($"Loading component: {component.Name} Version {component.Version}");
                startTasks.Add(component.StartAsync());
            }

            Console.WriteLine("All components started.");
            await Task.WhenAll(startTasks);

            List<Task> stopTasks = new List<Task>();
            foreach (IComponent component in components)
            {
                Console.WriteLine($"Unloading component: {component.Name} Version {component.Version}");
                stopTasks.Add(component.StopAsync());
            }

            await Task.WhenAll(stopTasks);
            Console.WriteLine("All components stopped.");
        }
    }
}
