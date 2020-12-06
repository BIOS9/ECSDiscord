using System;

namespace ComponentApplication.Components
{
    /// <summary>
    /// Represents an abstract component of the application.
    /// </summary>
    public interface IComponent : IInjectable
    {
        string Name { get; }
        Version Version { get; }
    }
}
