namespace Ocelot.Infrastructure.Claims.Parser
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using Errors;
    using Responses;

    public class ClaimsParser : IClaimsParser
    {
        public Response<string> GetValue(IEnumerable<Claim> claims, string key, string delimiter, int index)
        {
            var claimResponse = GetValue(claims, key);

            if (claimResponse.IsError)
            {
                return claimResponse;
            }

            if (string.IsNullOrEmpty(delimiter))
            {
                return claimResponse;
            }

            var splits = claimResponse.Data.Split(delimiter.ToCharArray());

            if (splits.Length < index || index < 0)
            {
                return new ErrorResponse<string>(new List<Error>
                {
                    new CannotFindClaimError($"Cannot find claim for key: {key}, delimiter: {delimiter}, index: {index}")
                });
            }

            var value = splits[index];

            return new OkResponse<string>(value);
        }

        private Response<string> GetValue(IEnumerable<Claim> claims, string key)
        {
            var claim = claims.FirstOrDefault(c => c.Type == key);

            if (claim != null)
            {
                return new OkResponse<string>(claim.Value);
            }

            return new ErrorResponse<string>(new List<Error>
            {
                new CannotFindClaimError($"Cannot find claim for key: {key}")
            });
        }
    }
}
