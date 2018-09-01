namespace Ocelot.Values
{
    public class UpstreamPathTemplate
    {
        public UpstreamPathTemplate(string template, int priority, bool containsQueryString, string originalValue)
        {
            Template = template;
            Priority = priority;
            ContainsQueryString = containsQueryString;
            OriginalValue = originalValue;
        }

        public string Template { get; }

        public int Priority { get; }

        public bool ContainsQueryString { get; }

        public string OriginalValue { get; }
    }
}
