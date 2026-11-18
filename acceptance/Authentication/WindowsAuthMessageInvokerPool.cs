using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Metadata;
using Ocelot.Requester;

namespace Ocelot.Acceptance.Authentication;

public class WindowsAuthMessageInvokerPool : MessageInvokerPool
{
    public WindowsAuthMessageInvokerPool(IDelegatingHandlerFactory handlerFactory, IOcelotLoggerFactory loggerFactory)
        : base(handlerFactory, loggerFactory) { }

    protected override SocketsHttpHandler CreateHandler(DownstreamRoute route)
    {
        var handler = base.CreateHandler(route);
        if (!route.HttpHandlerOptions.UseCredentials)
            return handler;

        var domain = route.GetMetadata<string>("auth/domain");
        var user = route.GetMetadata<string>("auth/user");
        var pswd = route.GetMetadata<string>("auth/password");
        if (!string.IsNullOrEmpty(user))
        {
            handler.Credentials = new NetworkCredential(user, pswd, domain);
        }
        return handler;
    }
}
