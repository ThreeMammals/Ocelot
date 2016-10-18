using System.Security.Claims;
using Ocelot.Responses;

namespace Ocelot.Authorisation
{
    public interface IAuthoriser
    {
        Response<bool> Authorise(ClaimsPrincipal claimsPrincipal, 
            RouteClaimsRequirement routeClaimsRequirement);
    }
}
