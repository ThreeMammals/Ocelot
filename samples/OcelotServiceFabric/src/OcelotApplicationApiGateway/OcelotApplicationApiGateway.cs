// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OcelotApplicationApiGateway
{
    using System.Fabric;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;
    using System.Collections.Generic;

    /// Service that handles front-end web requests and acts as a proxy to the back-end data for the UI web page.
    /// It is a stateless service that hosts a Web API application on OWIN.
    internal sealed class OcelotServiceWebService : StatelessService
    {
        public OcelotServiceWebService(StatelessServiceContext context)
            : base(context)
        { }

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new[]
            {
                new ServiceInstanceListener(
                    initparams => new WebCommunicationListener(string.Empty, initparams),
                    "OcelotServiceWebListener")
            };
        }
    }
}