namespace Ocelot.Values
{
    public class UpstreamPathTemplate
    {
        public UpstreamPathTemplate(string template, int priority, bool containsQueryString)
        {
            Template = template;
            Priority = priority;
            ContainsQueryString = containsQueryString;
        }

        public string Template { get; }

        public int Priority { get; }
        public bool ContainsQueryString { get; }
    }
}
