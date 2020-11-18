using System;
using System.Collections.Generic;
using System.Text;

namespace ComponentApplication.Components
{
    /// <summary>
    /// Represents an abstract component of the application.
    /// </summary>
    public interface IComponent
    {
        string Name { get; }
        Version Version { get; }
    }
}
