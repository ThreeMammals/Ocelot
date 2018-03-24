namespace Ocelot.Values
{
    public class UpstreamPathTemplate
    {
        public UpstreamPathTemplate(string template, int priority)
        {
            Template = template;
            Priority = priority;
        }

        public string Template { get; }

        public int Priority { get; }
    }
}
