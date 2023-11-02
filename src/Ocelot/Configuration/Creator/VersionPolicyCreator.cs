using System.Net.Http;

namespace Ocelot.Configuration.Creator
{
    public class VersionPolicyCreator : IVersionPolicyCreator
    {
        public HttpVersionPolicy Create(string downstreamVersionPolicy)
        {
            return downstreamVersionPolicy switch
            {
                VersionPolicies.Exact => HttpVersionPolicy.RequestVersionExact,
                VersionPolicies.Upgradeable => HttpVersionPolicy.RequestVersionOrHigher,
                VersionPolicies.Downgradable => HttpVersionPolicy.RequestVersionOrLower,
                _ => HttpVersionPolicy.RequestVersionOrLower,
            };
        }
    }
}
