using Microsoft.Extensions.Primitives;
using Ocelot.Responses;
using System.Security.Claims;

namespace Ocelot.Infrastructure.Claims.Parser
{
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

            if (splits.Length <= index || index < 0)
            {
                return new ErrorResponse<string>(new CannotFindClaimError($"Cannot find claim for key: {key}, delimiter: {delimiter}, index: {index}"));
            }

            var value = splits[index];

            return new OkResponse<string>(value);
        }

        public Response<List<string>> GetValuesByClaimType(IEnumerable<Claim> claims, string claimType)
        {
            var values = claims.Where(x => x.Type == claimType).Select(x => x.Value).ToList();

            return new OkResponse<List<string>>(values);
        }

        private static Response<string> GetValue(IEnumerable<Claim> claims, string key)
        {
            var claimValues = claims.Where(c => c.Type == key).Select(c => c.Value).ToArray();

            if (claimValues.Length > 0)
            {
                return new OkResponse<string>(new StringValues(claimValues).ToString());
            }

            return new ErrorResponse<string>(new CannotFindClaimError($"Cannot find claim for key: {key}"));
        }
    }
}
