namespace Ocelot.Library.Infrastructure.Configuration
{
    public interface IConfigurationReader
    {
        Configuration Read(string configurationFilePath);
    }
}
