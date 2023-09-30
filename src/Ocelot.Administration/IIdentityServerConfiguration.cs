namespace Ocelot.Administration
{
    public interface IIdentityServerConfiguration
    {
        string ApiName { get; }
        string ApiSecret { get; }
        bool RequireHttps { get; }
        List<string> AllowedScopes { get; }
        string CredentialsSigningCertificateLocation { get; }
        string CredentialsSigningCertificatePassword { get; }
    }
}
