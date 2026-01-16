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

goto Case5; // Your case should be selected here!!!

// Link: https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/kubernetes.rst#addkubernetes-bool-method
Case1: // Use a pod service account
builder.Services
    .AddOcelot(builder.Configuration)
    .AddKubernetes();
goto Start;

// Link: https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/kubernetes.rst#addkubernetes-bool-method
Case2: // Don't use a pod service account, manually bind options
Action<KubeClientOptions> configureOptions = opts =>
{
    opts.ApiEndPoint = new UriBuilder(Uri.UriSchemeHttps, "my-host", 443).Uri;
    opts.AccessToken = "my-token";
    opts.AuthStrategy = KubeAuthStrategy.BearerToken;
    opts.AllowInsecure = true;
};
builder.Services
    .AddOptions<KubeClientOptions>()
    .Configure(configureOptions); // mannual binding options via IOptions<KubeClientOptions>
builder.Services
    .AddOcelot().AddKubernetes(false); // don't use pod service account
goto Start;

// Link: https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/kubernetes.rst#addkubernetes-action-kubeclientoptions-method
Case3: // Don't use a pod service account, manually bind options, ignore global ServiceDiscoveryProvider json-options
Action<KubeClientOptions> myOptions = opts =>
{
    opts.ApiEndPoint = new UriBuilder(Uri.UriSchemeHttps, "my-host", 443).Uri;
    opts.AccessToken = "my-token";
    opts.AuthStrategy = KubeAuthStrategy.BearerToken;
    opts.AllowInsecure = true; // here is wrong value!
};
builder.Services
    .AddOcelot(builder.Configuration)
    .AddKubernetes(myOptions); // configure options with action, without optional args
goto Start;

Case4: // Don't use a pod service account, manually bind options, ignore global ServiceDiscoveryProvider json-options
builder.Services
    .AddKubeClientOptions(opts =>
    {
        opts.ApiEndPoint = new UriBuilder("https", "my-host", 443).Uri;
        opts.AuthStrategy = KubeAuthStrategy.BearerToken;
        opts.AccessToken = "my-token";
        opts.AllowInsecure = true;
    })
    .AddOcelot(builder.Configuration)
    .AddKubernetes(false); // don't use pod service account, and client options provided via AddKubeClientOptions
goto Start;

// Link: https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/kubernetes.rst#addkubernetes-action-kubeclientoptions-method
Case5: // Use global ServiceDiscoveryProvider json-options
Action<KubeClientOptions>? none = null;
builder.Services
    .AddOcelot(builder.Configuration)
    .AddKubernetes(null, allowInsecure: true /*optional args*/) // shorten version
    // or
    .AddKubernetes(none, allowInsecure: true /*optional args*/) // shorten version 2
    // or
    .AddKubernetes(configureOptions: null, allowInsecure: true /*optional args*/); // don't configure options with action, but do with optional args

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
await app.RunAsync();
