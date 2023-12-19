using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public class AuthenticationOptionsCreator : IAuthenticationOptionsCreator
    {
        public AuthenticationOptions Create(FileRoute route)
            => new(route?.AuthenticationOptions ?? new());
    }
}
