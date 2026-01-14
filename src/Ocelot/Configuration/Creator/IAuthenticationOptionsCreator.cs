using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public interface IAuthenticationOptionsCreator
{
    AuthenticationOptions Create(FileAuthenticationOptions options);
    AuthenticationOptions Create(FileRoute route, FileGlobalConfiguration globalConfiguration);
    AuthenticationOptions Create(FileDynamicRoute route, FileGlobalConfiguration globalConfiguration);
}
