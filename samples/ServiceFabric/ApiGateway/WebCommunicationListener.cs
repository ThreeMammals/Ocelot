using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Samples.Web;
using System.Fabric;
using System.Globalization;

namespace Ocelot.Samples.ServiceFabric.ApiGateway;

public class WebCommunicationListener : ICommunicationListener
{
    private readonly string _appRoot;
    private readonly ServiceContext _serviceInitializationParameters;
    private string _listeningAddress;
    private string _publishAddress;

    // OWIN server handle.
    private WebApplication _webApp;

    public WebCommunicationListener(string appRoot, ServiceContext serviceInitializationParameters)
    {
        _appRoot = appRoot;
        _serviceInitializationParameters = serviceInitializationParameters;
    }

    public async Task<string> OpenAsync(CancellationToken cancellationToken)
    {
        ServiceEventSource.Current.Message("Initialize");

        var serviceEndpoint = _serviceInitializationParameters.CodePackageActivationContext.GetEndpoint("WebEndpoint");
        var port = serviceEndpoint.Port;

        _listeningAddress = string.Format(
            CultureInfo.InvariantCulture,
            "http://+:{0}/{1}",
            port,
            string.IsNullOrWhiteSpace(_appRoot)
                ? string.Empty
                : _appRoot.TrimEnd('/') + '/');

        _publishAddress = _listeningAddress.Replace("+", FabricRuntime.GetNodeContext().IPAddressOrFQDN);

        ServiceEventSource.Current.Message("Starting web server on {0}", _listeningAddress);

        try
        {
            _ = OcelotHostBuilder.Create();
            var builder = WebApplication.CreateBuilder(); //(args);
            builder.WebHost.UseUrls(_listeningAddress);
            builder.Configuration
                .SetBasePath(builder.Environment.ContentRootPath)
                .AddOcelot();
            builder.Services
                .AddOcelot(builder.Configuration);
            if (builder.Environment.IsDevelopment())
            {
                builder.Logging.AddConsole();
            }

            _webApp = builder.Build();
            await _webApp.UseOcelot();
            await _webApp.RunAsync(); // .Start();
        }
        catch (Exception ex)
        {
            ServiceEventSource.Current.ServiceWebHostBuilderFailed(ex);
        }
        return _publishAddress;
    }

    public Task CloseAsync(CancellationToken cancellationToken) => StopAll(cancellationToken);
    public void Abort() => StopAll().GetAwaiter().GetResult();

    /// <summary>Stops, cancels, and disposes everything.</summary>
    private Task StopAll(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_webApp != null)
            {
                ServiceEventSource.Current.Message("Stopping web server.");
                return _webApp.StopAsync(cancellationToken);
            }
        }
        catch (ObjectDisposedException)
        {
        }
        return Task.CompletedTask;
    }
}
