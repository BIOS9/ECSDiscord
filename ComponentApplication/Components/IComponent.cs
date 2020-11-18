using System;
using System.Collections.Generic;
using System.Text;

namespace ComponentApplication.Components
{
    public interface IComponent
    {
        string Name { get; }
        Version Version { get; }
    }
}
