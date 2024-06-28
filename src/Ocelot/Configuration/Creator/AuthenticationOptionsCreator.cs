using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public class AuthenticationOptionsCreator : IAuthenticationOptionsCreator
    {
        public AuthenticationOptions Create(FileAuthenticationOptions routeAuthOptions, FileAuthenticationOptions globalConfAuthOptions)
        {
            var routeAuthOptionsEmpty = routeAuthOptions?.HasProviderKey() != true;
            var resultAuthOptions = routeAuthOptionsEmpty ? globalConfAuthOptions : routeAuthOptions;
            return new(resultAuthOptions ?? new());
        }
    }
}
