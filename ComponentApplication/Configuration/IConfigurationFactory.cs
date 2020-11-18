using System;
using System.Collections.Generic;
using System.Text;

namespace ComponentApplication.Configuration
{
    public interface IConfigurationFactory
    {
        T LoadConfiguration<T>(T defaultConfiguration);
    }
}
