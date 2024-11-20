using Ocelot.Responses;
using System.Security.Claims;

namespace Ocelot.Authorization;

public interface IRolesAuthorizer
{
    Response<bool> Authorize(ClaimsPrincipal claimsPrincipal, List<string> routeRequiredRole, string roleKey);
}
