namespace Ocelot.DependencyInjection
{
    public class NullAdministrationPath : IAdministrationPath
    {
        public NullAdministrationPath()
        {
            Path = null;
        }

        public string Path {get;private set;}
    }
}
