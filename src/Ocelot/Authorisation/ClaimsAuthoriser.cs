using System.Collections.Generic;
using System.Security.Claims;
using Ocelot.Errors;
using Ocelot.Responses;

namespace Ocelot.Authorisation
{
    using Infrastructure.Claims.Parser;

    public class ClaimsAuthoriser : IAuthoriser
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
                var value = _claimsParser.GetValue(claimsPrincipal.Claims, required.Key, string.Empty, 0);

                if (value.IsError)
                {
                    return new ErrorResponse<bool>(value.Errors);
                }

                if (value.Data != null)
                {
                    var authorised = value.Data == required.Value;
                    if (!authorised)
                    {
                        return new ErrorResponse<bool>(new List<Error>
                        {
                            new ClaimValueNotAuthorisedError(
                                $"claim value: {value.Data} is not the same as required value: {required.Value} for type: {required.Key}")
                        });
                    }
                }
                else
                {
                    return new ErrorResponse<bool>(new List<Error>
                        {
                            new UserDoesNotHaveClaimError($"user does not have claim {required.Key}")
                        });
                }
            }
            return new OkResponse<bool>(true);
        }
    }
}