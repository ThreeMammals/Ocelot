namespace Ocelot.Administration;

public interface IAdministrationPath
{
    string Path { get; }
    string IssuerSigningKey { get; }
    Uri ExternalJwtSigningUrl { get; }
}
