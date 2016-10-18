using System.Collections.Generic;

namespace Ocelot.Authorisation
{
    public class RouteClaimsRequirement
    {
        public RouteClaimsRequirement(Dictionary<string, string> requiredClaimsAndValues)
        {
            RequiredClaimsAndValues = requiredClaimsAndValues;
        }

        public Dictionary<string, string> RequiredClaimsAndValues { get; private set; } 
    }
}
