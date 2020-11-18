using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ComponentApplication.Components.Resources
{
    public interface IResourceLoader
    {
        void LoadAssemblies();
        ICollection<Assembly> GetAssemblies();
        Assembly AssemblyResolve(object sender, ResolveEventArgs args);
    }
}
