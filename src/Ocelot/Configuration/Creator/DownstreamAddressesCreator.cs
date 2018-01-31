using System.Collections.Generic;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public class DownstreamAddressesCreator : IDownstreamAddressesCreator
    {
        public List<DownstreamHostAndPort> Create(FileReRoute reRoute)
        {   
            var addresses = new List<DownstreamHostAndPort>();

            //todo - remove downstream stuff that isnt in list
            if(!string.IsNullOrEmpty(reRoute.DownstreamHost))
            {
                addresses.Add(new DownstreamHostAndPort(reRoute.DownstreamHost, reRoute.DownstreamPort));
            }

            foreach(var hostAndPort in reRoute.DownstreamHostAndPorts)
            {
                addresses.Add(new DownstreamHostAndPort(hostAndPort.DownstreamHost, hostAndPort.DownstreamPort));
            }

            return addresses;
        }
    }
}
