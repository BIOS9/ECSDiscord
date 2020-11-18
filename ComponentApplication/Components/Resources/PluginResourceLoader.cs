using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.RegularExpressions;

namespace ComponentApplication.Components.Resources
{
    /// <summary>
    /// Loads component resources/libraries.
    /// </summary>
    internal class PluginResourceLoader : IResourceLoader
    {
        private static readonly Regex versionRegex = new Regex("Version=(?<version>.+?), ", RegexOptions.Compiled);
        protected static string GetVersionIndependentName(string fullAssemblyName, out string extractedVersion)
        {
            var match = versionRegex.Match(fullAssemblyName);
            extractedVersion = match.Groups[1].Value;
            return versionRegex.Replace(fullAssemblyName, "");
        }

        private IDictionary<string, Assembly> _resourceAssemblies = new Dictionary<string, Assembly>();

        public void LoadAssemblies()
        {
            foreach (string file in Directory.GetFiles("Resources", "*.dll")) // Load each DLL in Resources directory.
            {
                Console.WriteLine("Loading Resource: " + file);
                Assembly assembly = Assembly.LoadFrom(Path.GetFullPath(file));
                string name = GetVersionIndependentName(assembly.FullName, out _);
                _resourceAssemblies.Add(name, assembly);
            }
        }

        public Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string name = GetVersionIndependentName(args.Name, out _);
            if (_resourceAssemblies.ContainsKey(name))
            {
                return _resourceAssemblies[name];
            }
            return null;
        }

        public ICollection<Assembly> GetAssemblies()
        {
            return _resourceAssemblies.Values;
        }
    }
}
