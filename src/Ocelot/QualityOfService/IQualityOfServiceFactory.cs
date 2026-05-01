using Ocelot.Configuration;
using Ocelot.Requester;

namespace Ocelot.QualityOfService;

public interface IQualityOfServiceFactory
{
    /// <summary>
    /// Gets the Quality of Service delegating handler for the downstream route.
    /// This is used to add the Quality of Service feature delegating handler to the pipeline for the downstream route.
    /// </summary>
    /// <remarks>
    /// 1. The list of <see cref="DelegatingHandler"/> objects created by the <see cref="IDelegatingHandlerFactory" /> for later reuse in the <see cref="MessageInvokerPool" />.<br/>
    /// 2. This method doesn't and shouldn't produce exceptions!
    /// If the Quality of Service feature is not enabled for the downstream route, it should return a delegating handler that doesn't do anything (e.g. a delegating handler that just calls the inner handler without doing anything else).
    /// </remarks>
    /// <param name="route">Current processing downstream route.</param>
    /// <returns>A <see cref="DelegatingHandler"/> object to be returned from the <see cref="IDelegatingHandlerFactory"/></returns>
    DelegatingHandler Get(DownstreamRoute route);
}
