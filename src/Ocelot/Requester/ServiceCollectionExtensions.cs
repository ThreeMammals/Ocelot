using Microsoft.Extensions.DependencyInjection;

namespace Ocelot.Requester;

public static class ServiceCollectionExtensions
{
    public static void AddOcelotMessageInvokerPool(this IServiceCollection services)
    {
        services.AddSingleton<IHttpRequester, MessageInvokerHttpRequester>();
        services.AddSingleton<IMessageInvokerPool, MessageInvokerPool>();
    }
}
