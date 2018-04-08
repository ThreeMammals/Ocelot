﻿using Ocelot.Configuration;
using Ocelot.Responses;

namespace Ocelot.DownstreamRouteFinder.Finder
{
    public interface IDownstreamRouteFinder
    {
        Response<DownstreamRoute> FindDownstreamRoute(string upstreamUrlPath, string upstreamHttpMethod, IOcelotConfiguration configuration, string upstreamHost);
    }
}
