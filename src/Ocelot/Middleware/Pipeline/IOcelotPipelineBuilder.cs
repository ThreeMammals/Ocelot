// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Removed code and changed RequestDelete to OcelotRequestDelete, HttpContext to DownstreamContext, removed some exception handling messages

using System;

namespace Ocelot.Middleware.Pipeline
{
    public interface IOcelotPipelineBuilder
    {
        IServiceProvider ApplicationServices { get; }

        IOcelotPipelineBuilder Use(Func<OcelotRequestDelegate, OcelotRequestDelegate> middleware);

        OcelotRequestDelegate Build();

        IOcelotPipelineBuilder New();
    }
}
