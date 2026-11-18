using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Logging;
using Ocelot.Requester;

namespace Ocelot.Testing.Requester;

public class RequesterSteps : AcceptanceSteps
{
    public static void WithRequesterTesting(IServiceCollection services)
        => WithRequesterTesting(services, true);

    public static void WithRequesterTesting(IServiceCollection services, bool addOcelot)
    {
        if (addOcelot) services.AddOcelot();
        services
            .AddSingleton<IOcelotTracer, TestOcelotTracer>()
            .AddOcelotRequester<TestMessageInvokerPool>();
    }
}
