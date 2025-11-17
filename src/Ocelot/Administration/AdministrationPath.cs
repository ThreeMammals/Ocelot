namespace Ocelot.Administration;

public class AdministrationPath : IAdministrationPath
{
    public AdministrationPath(string path, string apiSecret, Uri externalJwtServerUrl = null)
    {
        Path = path;
        IssuerSigningKey = apiSecret;
        ExternalJwtSigningUrl = externalJwtServerUrl;
    }

    public string Path { get; }
    public string IssuerSigningKey { get; }
    public Uri ExternalJwtSigningUrl { get; }
}
