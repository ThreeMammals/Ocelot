using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Ocelot.Configuration.Creator
{
    public interface IVersionPolicyCreator
    {
        HttpVersionPolicy Create(string downstreamVersionPolicy);
    }
}
