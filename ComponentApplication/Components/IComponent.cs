using System;
using System.Threading.Tasks;

namespace ComponentApplication.Components
{
    public interface IComponent
    {
        string Name { get; }
        Version Version { get; }

        Task LoadAsync();
        Task UnloadAsync();
    }
}