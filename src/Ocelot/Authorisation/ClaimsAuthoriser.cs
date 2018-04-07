using System.Collections.Generic;
using System.Security.Claims;
using Ocelot.Responses;

namespace Ocelot.Authorisation
{
    using Infrastructure.Claims.Parser;

    public class ClaimsAuthoriser : IClaimsAuthoriser
    {
        private readonly IClaimsParser _claimsParser;

        public ClaimsAuthoriser(IClaimsParser claimsParser)
        {
            _claimsParser = claimsParser;
        }

        public Response<bool> Authorise(ClaimsPrincipal claimsPrincipal, Dictionary<string, string> routeClaimsRequirement)
        {
            foreach (var required in routeClaimsRequirement)
            {
                var values = _claimsParser.GetValuesByClaimType(claimsPrincipal.Claims, required.Key);

                if (values.IsError)
                {
                    return new ErrorResponse<bool>(values.Errors);
                }

                if (values.Data != null)
                {
                    var authorised = values.Data.Contains(required.Value);
                    if (!authorised)
                    {
                        return new ErrorResponse<bool>(new ClaimValueNotAuthorisedError(
                                $"claim value: {values.Data} is not the same as required value: {required.Value} for type: {required.Key}"));
                    }
                }
                else
                {
                    return new ErrorResponse<bool>(new UserDoesNotHaveClaimError($"user does not have claim {required.Key}"));
                }
            }

            return new OkResponse<bool>(true);
        }
    }
}
