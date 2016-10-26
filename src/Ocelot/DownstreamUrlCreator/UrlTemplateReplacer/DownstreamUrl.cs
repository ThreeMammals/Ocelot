namespace Ocelot.DownstreamUrlCreator.UrlTemplateReplacer
{
    public class DownstreamUrl
    {
        public DownstreamUrl(string value)
        {
            Value = value;
        }

        public string Value { get; private set; }
    }
}
