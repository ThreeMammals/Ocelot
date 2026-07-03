using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ocelot.DependencyInjection;
using Ocelot.Logging;
using Ocelot.Requester;

namespace Ocelot.AcceptanceTests.Requester;

public class RequesterSteps : Steps
{
    public static void WithRequesterTesting(IServiceCollection services)
        => WithRequesterTesting(services, true);
    public static void WithRequesterTesting(IServiceCollection services, bool addOcelot)
    {
        if (addOcelot) services.AddOcelot();
        services
            .AddSingleton<IOcelotTracer, TestTracer>()
            .RemoveAll<IMessageInvokerPool>()
            .AddSingleton<IMessageInvokerPool, TestMessageInvokerPool>();
    }
}
