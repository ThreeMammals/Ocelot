// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OcelotApplicationApiGateway

{
    using System;
    using System.Fabric;
    using System.Threading;
    using Microsoft.ServiceFabric.Services.Runtime;
    using System.Diagnostics.Tracing;


    /// <summary>
    /// The service host is the executable that hosts the Service instances.
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            // Create Service Fabric runtime and register the service type.
            try
            {

                //Creating a new event listener to redirect the traces to a file
                ServiceEventListener listener = new ServiceEventListener("OcelotApplicationApiGateway");
                listener.EnableEvents(ServiceEventSource.Current, EventLevel.LogAlways, EventKeywords.All);

                // The ServiceManifest.XML file defines one or more service type names.
                // Registering a service maps a service type name to a .NET type.
                // When Service Fabric creates an instance of this service type,
                // an instance of the class is created in this host process.
                ServiceRuntime
                    .RegisterServiceAsync("OcelotApplicationApiGatewayType", context => new OcelotServiceWebService (context))
                        .GetAwaiter()
                        .GetResult();


                // Prevents this host process from terminating so services keep running.
                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(ex);
                throw ex;
            }
        }
    }
}