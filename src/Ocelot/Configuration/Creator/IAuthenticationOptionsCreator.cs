using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public interface IAuthenticationOptionsCreator
    {
        AuthenticationOptions Create(FileReRoute reRoute);
    }
}
