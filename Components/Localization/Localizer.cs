using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Localization
{
    internal class Localizer : IStringLocalizer
    {
        private readonly IDictionary<string, string> _stringMap = new Dictionary<string, string>();
        private readonly ILogger _logger;

        public Localizer(IDictionary<string, string> stringMap, ILogger logger)
        {
            _logger = logger;
            foreach(var s in stringMap) // Defensive cloning of input strings.
                _stringMap.Add(s.Key, s.Value);
        }

        public LocalizedString this[string name] => Get(name);
        public LocalizedString this[string name, params object[] arguments] => Get(name, arguments);

        public LocalizedString Get(string name, params object[] arguments)
        {
            if(_stringMap.ContainsKey(name))
                return new LocalizedString(name, string.Format(_stringMap[name], arguments));
            else
            {
                _logger.LogWarning("Cannot find localization record: {record}", name);
                return new LocalizedString(name, $"{name} [{string.Join(", ", arguments)}]", true);
            }      
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return _stringMap.Select(x => new LocalizedString(x.Key, x.Value));
        }
    }
}
