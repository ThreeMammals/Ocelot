using System.Security.Claims;

namespace Ocelot.Authorisation
{
    public interface IAuthoriser
    {
        bool Authorise(ClaimsPrincipal claimsPrincipal, 
            RouteClaimsRequirement routeClaimsRequirement);
    }
}
