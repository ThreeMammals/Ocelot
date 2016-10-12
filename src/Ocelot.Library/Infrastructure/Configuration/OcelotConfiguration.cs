using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Ocelot.Library.Infrastructure.Configuration.Yaml;

namespace Ocelot.Library.Infrastructure.Configuration
{
    public class OcelotConfiguration : IOcelotConfiguration
    {
        private readonly IOptions<YamlConfiguration> _options;
        private readonly List<ReRoute> _reRoutes;
        private const string RegExMatchEverything = ".*";
        private const string RegExMatchEndString = "$";

        public OcelotConfiguration(IOptions<YamlConfiguration> options)
        {
            _options = options;
            _reRoutes = new List<ReRoute>();
            SetReRoutes();
        }

        private void SetReRoutes()
        {
            foreach(var reRoute in _options.Value.ReRoutes)
            {
                var upstreamTemplate = reRoute.UpstreamTemplate;
                var placeholders = new List<string>();

                for (int i = 0; i < upstreamTemplate.Length; i++)
                {
                    if (IsPlaceHolder(upstreamTemplate, i))
                    {
                        var postitionOfPlaceHolderClosingBracket = upstreamTemplate.IndexOf('}', i);
                        var difference = postitionOfPlaceHolderClosingBracket - i + 1;
                        var variableName = upstreamTemplate.Substring(i, difference);
                        placeholders.Add(variableName);
                    }
                }

                foreach (var placeholder in placeholders)
                {
                    upstreamTemplate = upstreamTemplate.Replace(placeholder, RegExMatchEverything);
                }

                upstreamTemplate = $"{upstreamTemplate}{RegExMatchEndString}";

                var isAuthenticated = !string.IsNullOrEmpty(reRoute.Authentication);

                _reRoutes.Add(new ReRoute(reRoute.DownstreamTemplate, reRoute.UpstreamTemplate, reRoute.UpstreamHttpMethod, upstreamTemplate, isAuthenticated));
            }   
        }

        private static bool IsPlaceHolder(string upstreamTemplate, int i)
        {
            return upstreamTemplate[i] == '{';
        }

        public List<ReRoute> ReRoutes => _reRoutes;
    }
}