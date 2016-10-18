using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Ocelot.Errors;
using Ocelot.Responses;

namespace Ocelot.Configuration.Parser
{
    public class ClaimToHeaderConfigurationParser : IClaimToHeaderConfigurationParser
    {
        private readonly Regex _claimRegex = new Regex("Claims\\[.*\\]");
        private readonly Regex _indexRegex = new Regex("value\\[.*\\]");
        private const string SplitToken = ">";

        public Response<ClaimToHeader> Extract(string headerKey, string value)
        {
            try
            {
                var instructions = value.Split(SplitToken.ToCharArray());

                if (instructions.Length <= 1)
                {
                    return new ErrorResponse<ClaimToHeader>(
                        new List<Error>
                    {
                        new NoInstructionsError(SplitToken)
                    });
                }

                var claimMatch = _claimRegex.IsMatch(instructions[0]);

                if (!claimMatch)
                {
                    return new ErrorResponse<ClaimToHeader>(
                        new List<Error>
                        {
                            new InstructionNotForClaimsError()
                        });
                }

                var claimKey = GetIndexValue(instructions[0]);
                var index = 0;
                var delimiter = string.Empty;

                if (instructions.Length > 2 && _indexRegex.IsMatch(instructions[1]))
                {
                    index = int.Parse(GetIndexValue(instructions[1]));
                    delimiter = instructions[2].Trim();
                }

                return new OkResponse<ClaimToHeader>(
                               new ClaimToHeader(headerKey, claimKey, delimiter, index));
            }
            catch (Exception exception)
            {
                return new ErrorResponse<ClaimToHeader>(
                    new List<Error>
                    {
                        new ParsingConfigurationHeaderError(exception)
                    });
            }
        }

        private string GetIndexValue(string instruction)
        {
            var firstIndexer = instruction.IndexOf("[", StringComparison.Ordinal);
            var lastIndexer = instruction.IndexOf("]", StringComparison.Ordinal);
            var length = lastIndexer - firstIndexer;
            var claimKey = instruction.Substring(firstIndexer + 1, length - 1);
            return claimKey;
        }
    }
}
