using Ocelot.Infrastructure;
using Ocelot.Responses;

namespace Ocelot.Configuration.Parser;

/// <summary>
/// Default implementation of the <see cref="IClaimToThingConfigurationParser"/> interface.
/// </summary>
public partial class ClaimToThingConfigurationParser : IClaimToThingConfigurationParser
{
    private const char SplitToken = '>';

    [GeneratedRegex("Claims\\[.*\\]", RegexOptions.None, RegexGlobal.DefaultMatchTimeoutMilliseconds)]
    private static partial Regex ClaimRegex();

    [GeneratedRegex("value\\[.*\\]", RegexOptions.None, RegexGlobal.DefaultMatchTimeoutMilliseconds)]
    private static partial Regex IndexRegex();

    public Response<ClaimToThing> Extract(string existingKey, string value)
    {
        try
        {
            var instructions = value.Split(SplitToken);

            if (instructions.Length <= 1)
            {
                return new ErrorResponse<ClaimToThing>(new NoInstructionsError(SplitToken.ToString()));
            }

            var claimMatch = ClaimRegex().IsMatch(instructions[0]);

            if (!claimMatch)
            {
                return new ErrorResponse<ClaimToThing>(new InstructionNotForClaimsError());
            }

            var newKey = GetIndexValue(instructions[0]);
            var index = 0;
            var delimiter = string.Empty;

            if (instructions.Length > 2 && IndexRegex().IsMatch(instructions[1]))
            {
                index = int.Parse(GetIndexValue(instructions[1]));
                delimiter = instructions[2].Trim();
            }

            return new OkResponse<ClaimToThing>(
                           new ClaimToThing(existingKey, newKey, delimiter, index));
        }
        catch (Exception exception)
        {
            return new ErrorResponse<ClaimToThing>(new ParsingConfigurationHeaderError(exception));
        }
    }

    private static string GetIndexValue(string instruction)
    {
        var firstIndexer = instruction.IndexOf('[', StringComparison.Ordinal);
        var lastIndexer = instruction.IndexOf(']', StringComparison.Ordinal);
        var length = lastIndexer - firstIndexer;
        var claimKey = instruction.Substring(firstIndexer + 1, length - 1);
        return claimKey;
    }
}
