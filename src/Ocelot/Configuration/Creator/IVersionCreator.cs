namespace Ocelot.Configuration.Creator
{
    public interface IVersionCreator
    {
        Version Create(string downstreamHttpVersion);
    }
}
