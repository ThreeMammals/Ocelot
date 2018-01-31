using System.Collections.Generic;

namespace Ocelot.Raft
{
    [ExcludeFromCoverage]
    public class FilePeers
    {
        public FilePeers()
        {
            Peers = new List<FilePeer>();
        }

        public List<FilePeer> Peers {get; set;}
    }
}
