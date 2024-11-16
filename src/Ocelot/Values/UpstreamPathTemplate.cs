using Ocelot.Infrastructure;

namespace Ocelot.Values
{
    public class UpstreamPathTemplate
    {
        private static readonly Regex _reg = RegexGlobal.New("$^", RegexOptions.Singleline);
        private static readonly ConcurrentDictionary<string, Regex> _regex = new();

        public UpstreamPathTemplate(string template, int priority, bool containsQueryString, string originalValue)
        {
            Template = template;
            Priority = priority;
            ContainsQueryString = containsQueryString;
            OriginalValue = originalValue;
            Pattern = template == null
                ? _reg
                : _regex.AddOrUpdate(
                    template,
                    RegexGlobal.New(template, RegexOptions.Singleline),
                    (key, oldValue) => oldValue);
        }

        public string Template { get; }

        public int Priority { get; }

        public bool ContainsQueryString { get; }

        public string OriginalValue { get; }

        public Regex Pattern { get; }
    }
}
