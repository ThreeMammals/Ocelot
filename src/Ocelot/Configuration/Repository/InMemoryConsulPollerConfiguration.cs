namespace Ocelot.Configuration.Repository
{
    public class InMemoryConsulPollerConfiguration : IConsulPollerConfiguration
    {
        public int Delay => 1000;
    }
}
