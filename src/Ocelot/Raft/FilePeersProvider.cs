using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Ocelot.Configuration;
using Ocelot.Configuration.Provider;
using Rafty.Concensus;
using Rafty.Infrastructure;

namespace Ocelot.Raft
{
    public class FilePeersProvider : IPeersProvider
    {
        private readonly IOptions<FilePeers> _options;
        private List<IPeer> _peers;
        private IWebHostBuilder _builder;
        private IOcelotConfigurationProvider _provider;

        public FilePeersProvider(IOptions<FilePeers> options, IWebHostBuilder builder, IOcelotConfigurationProvider provider)
        {
            _provider = provider;
            _builder = builder;
            _options = options;
            _peers = new List<IPeer>();
            //todo - sort out async nonsense..
            var config = _provider.Get().GetAwaiter().GetResult();
            foreach (var item in _options.Value.Peers)
            {
                var httpClient = new HttpClient();
                //todo what if this errors?
                var httpPeer = new HttpPeer(item.HostAndPort, Guid.Parse(item.Id), httpClient, _builder, config.Data);
                _peers.Add(httpPeer);
            }
        }
        public List<IPeer> Get()
        {
            return _peers;
        }
    }
}
