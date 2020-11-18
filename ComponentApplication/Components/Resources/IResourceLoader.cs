using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ComponentApplication.Components.Resources
{
    public interface IResourceLoader
    {
        /// <summary>
        /// Load resource assemblies into the resource loader.
        /// </summary>
        void LoadAssemblies();

        /// <summary>
        /// Get the assemblies that have been loaded into the resource loader.
        /// </summary>
        /// <returns>Collection of loaded assemblies.</returns>
        ICollection<Assembly> GetAssemblies();

        /// <summary>
        /// Assembly resolve event handler used to allow the <see cref="AppDomain"/> to load assemblies from the loader. 
        /// </summary>
        /// <param name="sender">Object that called the resolve callback.</param>
        /// <param name="args">Resolve event arguments.</param>
        /// <returns>Resource assembly.</returns>
        Assembly AssemblyResolve(object sender, ResolveEventArgs args);
    }
}
