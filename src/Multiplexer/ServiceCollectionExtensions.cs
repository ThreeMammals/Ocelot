using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ocelot.Configuration.Creator;
using Ocelot.DependencyInjection;

namespace Ocelot.Multiplexer;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOcelotAggregation<TAggregatesCreator>(this IServiceCollection services)
        where TAggregatesCreator : class, IAggregatesCreator
        => Features.AddOcelotAggregation(services)
        .RemoveAll<IAggregatesCreator>().AddSingleton<IAggregatesCreator, TAggregatesCreator>();
}
