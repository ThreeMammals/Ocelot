namespace Ocelot.Configuration.Creator
{
    public class HttpVersionCreator : IVersionCreator
    {
        public Version Create(string downstreamHttpVersion)
        {
            if (!Version.TryParse(downstreamHttpVersion, out var version))
            {
                version = new Version(1, 1);
            }

            return version;
        }
    }
}
