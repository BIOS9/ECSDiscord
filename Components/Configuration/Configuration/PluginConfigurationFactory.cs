using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ComponentApplication.Configuration
{
    internal class PluginConfigurationFactory : IConfigurationFactory
    {
        public T LoadConfiguration<T>(T defaultConfiguration)
        {
            throw new NotImplementedException();
        }
    }
}
