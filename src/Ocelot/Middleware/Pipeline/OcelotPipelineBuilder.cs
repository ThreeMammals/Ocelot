// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Removed code and changed RequestDelete to OcelotRequestDelete, HttpContext to DownstreamContext, removed some exception handling messages

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ocelot.Middleware.Pipeline
{
    public class OcelotPipelineBuilder : IOcelotPipelineBuilder
    {
        private readonly IList<Func<OcelotRequestDelegate, OcelotRequestDelegate>> _middlewares;

        public OcelotPipelineBuilder(IServiceProvider provider)
        {
            ApplicationServices = provider;
            _middlewares = new List<Func<OcelotRequestDelegate, OcelotRequestDelegate>>();
        }

        public OcelotPipelineBuilder(IOcelotPipelineBuilder builder)
        {
            ApplicationServices = builder.ApplicationServices;
            _middlewares = new List<Func<OcelotRequestDelegate, OcelotRequestDelegate>>();
        }

        public IServiceProvider ApplicationServices { get; }

        public IOcelotPipelineBuilder Use(Func<OcelotRequestDelegate, OcelotRequestDelegate> middleware)
        {
            _middlewares.Add(middleware);
            return this;
        }

        public OcelotRequestDelegate Build()
        {
            OcelotRequestDelegate app = context =>
            {
                context.HttpContext.Response.StatusCode = 404;
                return Task.CompletedTask;
            };

            foreach (var component in _middlewares.Reverse())
            {
                app = component(app);
            }

            return app;
        }

        public IOcelotPipelineBuilder New()
        {
            return new OcelotPipelineBuilder(this);
        }
    }
}
