namespace Ocelot.Library.Configuration
{
    public class ClaimToHeader
    {
        public ClaimToHeader(string headerKey, string claimKey, string delimiter, int index)
        {
            ClaimKey = claimKey;
            Delimiter = delimiter;
            Index = index;
            HeaderKey = headerKey;
        }

        public string HeaderKey { get; private set; }
        public string ClaimKey { get; private set; }
        public string Delimiter { get; private set; }
        public int Index { get; private set; }
    }
}
