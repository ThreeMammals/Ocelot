using Ocelot.DependencyInjection;
using Ocelot.Middleware;

namespace Ocelot.Samples.MultipartFormData;

public static class ApiGateway
{
    public static Task<WebApplication> New(int? port, string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration
            .SetBasePath(builder.Environment.ContentRootPath)
            .AddOcelot();
        builder.Services
            .AddOcelot(builder.Configuration);

        if (builder.Environment.IsDevelopment())
        {
            builder.Logging.AddConsole();
        }

        var gateway = builder.Build();

        // gateway.Urls.Clear();
        // gateway.Urls.Add(new UriBuilder(Uri.UriSchemeHttp, "localhost", port).ToString());
        // USE URLs from launchSettings.json

        gateway.UseDefaultFiles();
        gateway.UseStaticFiles();
        return gateway.UseOcelot()
            .ContinueWith(t => gateway);
    }
}
