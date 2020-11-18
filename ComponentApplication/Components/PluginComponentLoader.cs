using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ComponentApplication.Components
{
    /// <summary>
    /// Loads components from plugin DLL files.
    /// </summary>
    internal class PluginComponentLoader : IComponentLoader
    {
        public Assembly[] LoadAssemblies()
        {
            List<Assembly> assemblies = new List<Assembly>();
            foreach (string file in Directory.GetFiles("Plugins", "*.dll")) // Load each DLL in Plugins directory.
            {
                Console.WriteLine("Loading Plugin: " + file);
                assemblies.Add(Assembly.LoadFrom(file));
            }
            return assemblies.ToArray();
        }
    }
}
