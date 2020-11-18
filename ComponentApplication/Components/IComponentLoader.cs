using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ComponentApplication.Components
{
    /// <summary>
    /// Loads application component assemblies.
    /// </summary>
    public interface IComponentLoader
    { 
        /// <summary>
        /// Loads assemblies containing components.
        /// </summary>
        /// <returns>Array of assemblies that contain components.</returns>
        Assembly[] LoadAssemblies();
    }
}
