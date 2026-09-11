using Ocelot.Configuration;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Infrastructure;
using Ocelot.Infrastructure.Claims;
using Ocelot.Responses;
using Ocelot.Values;
using System.Security.Claims;

namespace Ocelot.PathManipulation;

public class ChangeDownstreamPathTemplate : IChangeDownstreamPathTemplate
{
    private readonly IClaimsParser _claimsParser;

    public ChangeDownstreamPathTemplate(IClaimsParser claimsParser)
    {
        _claimsParser = claimsParser;
    }

    public Response ChangeDownstreamPath(List<ClaimToThing> claimsToThings, IEnumerable<Claim> claims,
        DownstreamPathTemplate downstreamPathTemplate, List<PlaceholderNameAndValue> placeholders)
    {
        foreach (var config in claimsToThings)
        {
            var value = _claimsParser.GetValue(claims, config.NewKey, config.Delimiter, config.Index);
            if (value.IsError)
                return new ErrorResponse(value.Errors);

            var placeholder = new PlaceholderNameAndValue(config.ExistingKey, value.Data, true);

            if (!downstreamPathTemplate.Value.Contains(placeholder.Name))
                return new ErrorResponse(new CouldNotFindPlaceholderError(placeholder.Name));

            if (placeholders.Any(ph => ph.Name == placeholder.Name)) // TODO Implement equality operators and IEquatable
                placeholders.RemoveAll(ph => ph.Name == placeholder.Name);

            placeholders.Add(placeholder);
        }

        return new OkResponse();
    }
}
