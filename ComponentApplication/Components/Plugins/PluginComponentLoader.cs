using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ComponentApplication.Components.Plugins
{
    internal class PluginComponentLoader : IComponentLoader
    {
        public Assembly[] LoadAssemblies()
        {
            List<Assembly> assemblies = new List<Assembly>();
            foreach (string file in Directory.GetFiles("Plugins", "*.dll"))
            {
                Console.WriteLine("Loading Plugin: " + file);
                assemblies.Add(Assembly.LoadFrom(file));
            }
            return assemblies.ToArray();
        }

        public Task<Assembly[]> LoadAssembliesAsync()
        {
            throw new NotImplementedException();
        }
    }
}
