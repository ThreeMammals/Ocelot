using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.Repository;
using Ocelot.Logging;

namespace Ocelot.Middleware;

public class ConfigurationMiddleware : OcelotMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IInternalConfigurationRepository _configRepo;

    public ConfigurationMiddleware(RequestDelegate next, IOcelotLoggerFactory loggerFactory, IInternalConfigurationRepository configRepo)
        : base(loggerFactory.CreateLogger<ConfigurationMiddleware>())
    {
        _next = next;
        _configRepo = configRepo;
    }

    public async Task Invoke(HttpContext context)
    {
        //todo check the config is actually ok?
        var config = _configRepo.Get();
        if (config != null)
            context.Items.SetIInternalConfiguration(config);

        await _next.Invoke(context);
    }
}
