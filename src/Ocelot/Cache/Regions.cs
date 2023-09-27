namespace Ocelot.Cache
{
    public class Regions
    {
        public Regions(List<string> value)
        {
            Value = value;
        }

        public List<string> Value { get; }
    }
}
