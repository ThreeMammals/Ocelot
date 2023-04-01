// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Modified https://github.com/aspnet/Proxy websockets class to use in Ocelot.

using Ocelot.Middleware;

namespace Ocelot.WebSockets.Middleware
{
    public interface IOcelotWebSocketsProxyMiddleware : IOcelotMiddleware
    {
    }
}
