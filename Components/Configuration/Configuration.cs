using ComponentApplication.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Configuration
{
    public class Configuration : IComponent, IConfigurationRoot
    {
        public string Name => "Plugin Configuration";
        public Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        public string this[string key] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private readonly IConfigurationRoot _configurationRoot;

        public Configuration()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddJsonFile("config.json");

            _configurationRoot = builder.Build();
        }

        public IEnumerable<IConfigurationProvider> Providers => _configurationRoot.Providers;
        public IEnumerable<IConfigurationSection> GetChildren() => _configurationRoot.GetChildren();
        public IChangeToken GetReloadToken() => _configurationRoot.GetReloadToken();
        public IConfigurationSection GetSection(string key) => _configurationRoot.GetSection(key);
        public void Reload() => _configurationRoot.Reload();
    }
}
