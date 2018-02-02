namespace Ocelot.Values
{
    public class PathTemplate
    {
        public PathTemplate(string value)
        {
            Value = value;
        }

        public string Value { get; private set; }
    }
}
