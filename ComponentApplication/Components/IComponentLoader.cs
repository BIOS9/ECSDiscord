using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ComponentApplication.Components
{
    internal interface IComponentLoader
    {
        Task<Assembly[]> LoadAssembliesAsync();
        Assembly[] LoadAssemblies();
    }
}
