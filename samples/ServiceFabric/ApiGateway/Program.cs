using Microsoft.ServiceFabric.Services.Runtime;
using Ocelot.Samples.ServiceFabric.ApiGateway;
using System.Diagnostics.Tracing;

// The service host is the executable that hosts the Service instances.
// Create Service Fabric runtime and register the service type.
try
{
    //Creating a new event listener to redirect the traces to a file
    var listener = new ServiceEventListener("OcelotApplicationApiGateway");
    listener.EnableEvents(ServiceEventSource.Current, EventLevel.LogAlways, EventKeywords.All);

    // The ServiceManifest.XML file defines one or more service type names.
    // Registering a service maps a service type name to a .NET type.
    // When Service Fabric creates an instance of this service type,
    // an instance of the class is created in this host process.
    await ServiceRuntime.RegisterServiceAsync("OcelotApplicationApiGatewayType", context => new OcelotServiceWebService(context));

    // Prevents this host process from terminating so services keep running.
    Thread.Sleep(Timeout.Infinite);
}
catch (Exception ex)
{
    ServiceEventSource.Current.ServiceHostInitializationFailed(ex);
    throw;
}
