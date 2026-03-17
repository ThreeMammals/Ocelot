namespace Ocelot.Testing.Authentication;

public class AuthenticationTokenRequestEventArgs(AuthenticationTokenRequest request) : EventArgs
{
    public AuthenticationTokenRequest Request { get; } = request;
}
