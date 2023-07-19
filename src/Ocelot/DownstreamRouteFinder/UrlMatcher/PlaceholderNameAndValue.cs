namespace Ocelot.DownstreamRouteFinder.UrlMatcher
{
    public class PlaceholderNameAndValue
    {
        public PlaceholderNameAndValue(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public string Value { get; }
    }
}
