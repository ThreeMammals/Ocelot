using System.Collections.Generic;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public class DownstreamAddressesCreator : IDownstreamAddressesCreator
    {
        public List<DownstreamAddress> Create(FileReRoute reRoute)
        {   
            var addresses = new List<DownstreamAddress>();

            //todo - remove downstream stuff that isnt in list
            if(!string.IsNullOrEmpty(reRoute.DownstreamHost))
            {
                addresses.Add(new DownstreamAddress(reRoute.DownstreamHost, reRoute.DownstreamPort));
            }

            foreach(var hostAndPort in reRoute.DownstreamHostAndPorts)
            {
                addresses.Add(new DownstreamAddress(hostAndPort.DownstreamHost, hostAndPort.DownstreamPort));
            }

            return addresses;
        }
    }
}
