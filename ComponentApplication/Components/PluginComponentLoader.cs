using ComponentApplication.Components.Resources;
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
        private const string PluginDirectory = "Plugins";
        private readonly IDictionary<Assembly, IResourceLoader> _loadContexts = new Dictionary<Assembly, IResourceLoader>();

        public PluginComponentLoader()
        {
            AppDomain.CurrentDomain.AssemblyResolve += assemblyResolve;
        }

        private Assembly assemblyResolve(object sender, ResolveEventArgs args)
        {
            if (_loadContexts.ContainsKey(args.RequestingAssembly))
            {
                return _loadContexts[args.RequestingAssembly].AssemblyResolve(sender, args);
            }
            return null;
        }

        public Assembly[] LoadAssemblies()
        {
            List<Assembly> assemblies = new List<Assembly>();
            foreach (string file in Directory.GetFiles(PluginDirectory, "*.dll")) // Load each DLL in Plugins directory.
            {
                string fullPath = Path.GetFullPath(file);
                string pluginName = Path.GetFileNameWithoutExtension(file);
                Console.WriteLine("Loading Plugin: " + file);

                Assembly assembly = Assembly.LoadFrom(fullPath);
                assemblies.Add(assembly);

                string resourceDirectory = Path.Join(PluginDirectory, pluginName, "Resources");
                if (!Directory.Exists(PluginDirectory))
                    Directory.CreateDirectory(PluginDirectory);
                if (!Directory.Exists(resourceDirectory))
                    Directory.CreateDirectory(resourceDirectory);

                _loadContexts.Add(assembly, new PluginResourceLoader(resourceDirectory, pluginName, false));
            }
            return assemblies.ToArray();
        }
    }
}
