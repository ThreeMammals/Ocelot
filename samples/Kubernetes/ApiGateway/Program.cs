using KubeClient;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Kubernetes;
using Ocelot.Samples.Web;

//_ = OcelotHostBuilder.Create(args);
var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddOcelot();

goto Case4; // Your case should be selected here!!!

Case1: // Use a pod service account. Link: https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/kubernetes.rst#addkubernetes-bool-method
builder.Services
    .AddOcelot(builder.Configuration)
    .AddKubernetes();
goto Start;

Case2: // Don't use a pod service account, manually bind options. Link: https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/kubernetes.rst#addkubernetes-bool-method
Action<KubeClientOptions> configureOptions = opts => 
{ 
    opts.ApiEndPoint = new UriBuilder(Uri.UriSchemeHttps, "my-host", 443).Uri;
    opts.AccessToken = "my-token";
    opts.AuthStrategy = KubeAuthStrategy.BearerToken;
    opts.AllowInsecure = true; 
};
builder.Services
    .Configure(configureOptions) // manual binding options via IOptions<KubeClientOptions>
    .AddOcelot().AddKubernetes(false); // don't use pod service account
goto Start;

Case3: // Use global ServiceDiscoveryProvider json-options. Link: https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/kubernetes.rst#addkubernetes-action-kubeclientoptions-method
Action<KubeClientOptions> myOptions = opts =>
{
    opts.ApiEndPoint = new UriBuilder(Uri.UriSchemeHttps, "my-host", 443).Uri;
    opts.AccessToken = "my-token";
    opts.AuthStrategy = KubeAuthStrategy.BearerToken;
    opts.AllowInsecure = false; // here is wrong value!
};
builder.Services
    .AddOcelot(builder.Configuration)
    .AddKubernetes(myOptions, allowInsecure: true /*optional args*/); // configure options with factory-action, and do with optional args
goto Start;

Case4: // Use global ServiceDiscoveryProvider json-options. Link: https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/kubernetes.rst#addkubernetes-action-kubeclientoptions-method
builder.Services
    .AddOcelot(builder.Configuration)
    .AddKubernetes(null, allowInsecure: true /*optional args*/) // shorten version
    .AddKubernetes(configureOptions: null, allowInsecure: true /*optional args*/); // don't configure options with factory-action, but do with optional args

Start:

if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddConsole();
}

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
await app.UseOcelot();
app.Run();
