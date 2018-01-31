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
    [ExcludeFromCoverage]
    public class FilePeersProvider : IPeersProvider
    {
        private readonly IOptions<FilePeers> _options;
        private List<IPeer> _peers;
        private IWebHostBuilder _builder;
        private IOcelotConfigurationProvider _provider;
        private IIdentityServerConfiguration _identityServerConfig;

        public FilePeersProvider(IOptions<FilePeers> options, IWebHostBuilder builder, IOcelotConfigurationProvider provider, IIdentityServerConfiguration identityServerConfig)
        {
            _identityServerConfig = identityServerConfig;
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
                var httpPeer = new HttpPeer(item.HostAndPort, httpClient, _builder, config.Data, _identityServerConfig);
                _peers.Add(httpPeer);
            }
        }
        public List<IPeer> Get()
        {
            return _peers;
        }
    }
}
