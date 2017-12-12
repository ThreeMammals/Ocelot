using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Options;
using Rafty.Concensus;
using Rafty.Infrastructure;

namespace Ocelot.Raft
{
    public class FilePeersProvider : IPeersProvider
    {
        private readonly IOptions<FilePeers> _options;
        private List<IPeer> _peers;

        public FilePeersProvider(IOptions<FilePeers> options)
        {
            _options = options;
            _peers = new List<IPeer>();
            foreach (var item in _options.Value.Peers)
            {
                var httpClient = new HttpClient();
                var httpPeer = new HttpPeer(item.HostAndPort, Guid.Parse(item.Id), httpClient);
                _peers.Add(httpPeer);
            }
        }
        public List<IPeer> Get()
        {
            return _peers;
        }
    }
}
