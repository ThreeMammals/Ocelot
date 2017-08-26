using Ocelot.Configuration;
using Ocelot.Configuration.File;

namespace Ocelot.Creator.Configuration
{
    public interface IAuthenticationProviderConfigCreator
    {
        IAuthenticationConfig Create(FileAuthenticationOptions authenticationOptions);
    }
}