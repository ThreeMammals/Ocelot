namespace Ocelot.Requester;

public class TimeoutDelegatingHandler : DelegatingHandler
{
    private readonly TimeSpan _timeout;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeoutDelegatingHandler"/> class.
    /// </summary>
    /// <param name="timeout">The time span after which the request is cancelled.</param>
    public TimeoutDelegatingHandler(TimeSpan timeout)
    {
        _timeout = timeout;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_timeout);

        try
        {
            return await base.SendAsync(request, cts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException();
        }
    }
}
