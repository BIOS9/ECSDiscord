using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;

namespace ComponentApplication.Components.Resources
{
    /// <summary>
    /// Loads component resources/libraries.
    /// </summary>
    internal class PluginResourceLoader : IResourceLoader
    {
        private static readonly Regex versionRegex = new Regex("Version=(?<version>.+?), ", RegexOptions.Compiled); // Regex for the version number in the assembly name.

        private readonly AssemblyLoadContext _loadContext;
        private readonly IDictionary<string, Assembly> _resouceAssemblies = new Dictionary<string, Assembly>();
        private readonly bool _ignoreVersion;

        public PluginResourceLoader(string directory, string scopeName, bool ignoreVersion = false)
        {
            _ignoreVersion = ignoreVersion;
            _loadContext = new AssemblyLoadContext(scopeName);
            foreach (string file in Directory.GetFiles(directory, "*.dll")) // Load each DLL in Resources directory.
            {
                Assembly assembly = _loadContext.LoadFromAssemblyPath(Path.GetFullPath(file));
                string name = ignoreVersion ? getVersionIndependentName(assembly.FullName, out _) : assembly.FullName;
                _resouceAssemblies.Add(name, assembly);
            }
        }

        public Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string name = _ignoreVersion ? getVersionIndependentName(args.Name, out _) : args.Name;

            if (_resouceAssemblies.ContainsKey(name))
            {
                return _resouceAssemblies[name];
            }
            return null;
        }

        public ICollection<Assembly> GetAssemblies()
        {
            return _resouceAssemblies.Values;
        }

        /// <summary>
        /// Get the full name of an assembly with the version information removed.
        /// </summary>
        /// <param name="fullAssemblyName">Full name of an assembly.</param>
        /// <param name="extractedVersion">Version that was removed.</param>
        /// <returns>Name string with version removed.</returns>
        /// <remarks>Source Rocket.Unturned: https://github.com/RocketMod/Rocket.Unturned/blob/5b684f782678c740006c844a79d17a36d2babefe/Rocket.Unturned.Module/RocketUnturnedModule.cs#L146 </remarks>
        private static string getVersionIndependentName(string fullAssemblyName, out string extractedVersion)
        {
            var match = versionRegex.Match(fullAssemblyName);
            extractedVersion = match.Groups[1].Value;
            return versionRegex.Replace(fullAssemblyName, "");
        }
    }
}
