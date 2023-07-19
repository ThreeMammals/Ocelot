namespace Ocelot.Configuration
{
    public class ClaimToThing
    {
        public ClaimToThing(string existingKey, string newKey, string delimiter, int index)
        {
            NewKey = newKey;
            Delimiter = delimiter;
            Index = index;
            ExistingKey = existingKey;
        }

        public string ExistingKey { get; }
        public string NewKey { get; }
        public string Delimiter { get; }
        public int Index { get; }
    }
}
