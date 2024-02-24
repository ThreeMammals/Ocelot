using System.Net.Http;

namespace Ocelot.Configuration.Creator
{
    public class VersionPolicyCreator : IVersionPolicyCreator
    {
        public HttpVersionPolicy Create(string downstreamVersionPolicy)
        {
            return downstreamVersionPolicy switch
            {
                VersionPolicies.RequestVersionExact => HttpVersionPolicy.RequestVersionExact,
                VersionPolicies.RequestVersionOrHigher => HttpVersionPolicy.RequestVersionOrHigher,
                VersionPolicies.RequestVersionOrLower => HttpVersionPolicy.RequestVersionOrLower,
                _ => HttpVersionPolicy.RequestVersionOrLower,
            };
        }
    }
}
