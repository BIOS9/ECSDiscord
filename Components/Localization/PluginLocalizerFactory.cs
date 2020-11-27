using ComponentApplication.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Localization
{
    internal class PluginLocalizerFactory : IComponent, IStringLocalizerFactory
    {
        public string Name => "Plugin Localization";
        public Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        private readonly ILogger _logger;

        public PluginLocalizerFactory(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger("Localizer");
        }

        public IStringLocalizer Create(Type resourceSource)
        {
            string resourceName = string.Empty;
            foreach (var res in resourceSource.Assembly.GetManifestResourceNames())
            {
                if (res.EndsWith("Localization.json"))
                {
                    resourceName = res;
                    break;
                }
            }

            if (resourceName == string.Empty) // No localization data exists for the plugin.
                return new Localizer(new Dictionary<string, string>(), _logger); // Return empty dictionary.

            // Read localization file from embedded resource of assembly.
            using (StreamReader resource = new StreamReader(resourceSource.Assembly.GetManifestResourceStream(resourceName)))
            {
                var stringMap = JsonSerializer.Deserialize<IDictionary<string, string>>(resource.ReadToEnd()); // Convert JSON to dictionary
                return new Localizer(stringMap, _logger);
            }
        }

        public IStringLocalizer Create(string baseName, string location)
        {
            throw new NotImplementedException();
        }
    }
}
