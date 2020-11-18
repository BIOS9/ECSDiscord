using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ComponentApplication.Components.Resources
{
    /// <summary>
    /// Loads component resources/libraries.
    /// </summary>
    internal class PluginResourceLoader : IResourceLoader
    {
        private static readonly Regex versionRegex = new Regex("Version=(?<version>.+?), ", RegexOptions.Compiled); // Regex for the version number in the assembly name.

        private IDictionary<string, Assembly> _resourceAssemblies = new Dictionary<string, Assembly>();

        /// <summary>
        /// Get the full name of an assembly with the version information removed.
        /// </summary>
        /// <param name="fullAssemblyName">Full name of an assembly.</param>
        /// <param name="extractedVersion">Version that was removed.</param>
        /// <returns>Name string with version removed.</returns>
        /// <remarks>Source Rocket.Unturned: https://github.com/RocketMod/Rocket.Unturned/blob/5b684f782678c740006c844a79d17a36d2babefe/Rocket.Unturned.Module/RocketUnturnedModule.cs#L146 </remarks>
        private static string GetVersionIndependentName(string fullAssemblyName, out string extractedVersion)
        {
            var match = versionRegex.Match(fullAssemblyName);
            extractedVersion = match.Groups[1].Value;
            return versionRegex.Replace(fullAssemblyName, "");
        }

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
