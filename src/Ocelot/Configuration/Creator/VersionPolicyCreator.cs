using System.Net.Http;

namespace Ocelot.Configuration.Creator
{
    public class VersionPolicyCreator : IVersionPolicyCreator
    {
        public HttpVersionPolicy Create(string downstreamVersionPolicy)
        {
            return downstreamVersionPolicy switch
            {
                "exact" => HttpVersionPolicy.RequestVersionExact,
                "upgradeable" => HttpVersionPolicy.RequestVersionOrHigher,
                "downgradeable" => HttpVersionPolicy.RequestVersionOrLower,
                _ => HttpVersionPolicy.RequestVersionOrLower,
            };
        }
    }
}
