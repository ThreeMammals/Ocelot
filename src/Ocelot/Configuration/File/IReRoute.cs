namespace Ocelot.Configuration.File
{
    public interface IReRoute
    {
        string UpstreamPathTemplate { get; set; }
        bool ReRouteIsCaseSensitive { get; set; }
        int Priority { get; set; }
    }
}
