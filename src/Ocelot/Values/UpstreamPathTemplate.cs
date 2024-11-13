namespace Ocelot.Values
{
    public class UpstreamPathTemplate
    {
        private static readonly Regex _reg = new Regex("$^", RegexOptions.Compiled | RegexOptions.Singleline, RegexGlobal.DefaultMatchTimeout);
        private static ConcurrentDictionary<string, Regex> _regex = new ConcurrentDictionary<string, Regex>();

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
                    new Regex(template, RegexOptions.Compiled | RegexOptions.Singleline, RegexGlobal.DefaultMatchTimeout),
                    (key, oldValue) => oldValue);
        }

        public string Template { get; }

        public int Priority { get; }

        public bool ContainsQueryString { get; }

        public string OriginalValue { get; }

        public Regex Pattern { get; }
    }
}
