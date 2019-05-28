using Ocelot.Errors;

namespace Ocelot.Middleware.Multiplexer
{
    public class CouldNotFindAggregatorError : Error
    {
        public CouldNotFindAggregatorError(string aggregator)
            : base($"Could not find Aggregator: {aggregator}", OcelotErrorCode.CouldNotFindAggregatorError)
        {
        }
    }
}
