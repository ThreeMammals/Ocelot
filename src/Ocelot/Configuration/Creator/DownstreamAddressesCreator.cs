using Ocelot.Configuration.File;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ocelot.Configuration.Creator
{
    public class DownstreamAddressesCreator : IDownstreamAddressesCreator
    {
        public List<DownstreamHostAndPort> Create(FileRoute fileRoute, Dictionary<string, FileCluster> clusters)
        {

            if (!string.IsNullOrWhiteSpace(fileRoute.ServiceName))
            {
                var cluster = clusters[fileRoute.ClusterId];

                //TODO: extract this and test
                return cluster.Destinations.Select(d =>
                {
                    var uri = new Uri(d.Value.Address);

                    return new DownstreamHostAndPort(uri.Scheme, uri.Host, uri.Port);
                }).ToList();
            } 
            else
            {

            }
        }
    }
}
