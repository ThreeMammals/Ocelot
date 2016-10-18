using System.Security.Claims;

namespace Ocelot.Authorisation
{
    public class ClaimsAuthoriser : IAuthoriser
    {
        public bool Authorise(ClaimsPrincipal claimsPrincipal, RouteClaimsRequirement routeClaimsRequirement)
        {
            return false;
        }
    }
}