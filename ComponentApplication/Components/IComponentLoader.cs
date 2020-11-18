using System.Reflection;

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
