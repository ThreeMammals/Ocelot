namespace Ocelot.Configuration.File;

public class FileRateLimitByHeaderRule
{
    public FileRateLimitByHeaderRule()
    {
        ClientWhitelist = new List<string>();
    }

    public FileRateLimitByHeaderRule(FileRateLimitByHeaderRule from)
    {
        ClientWhitelist = new List<string>(from.ClientWhitelist);
        EnableRateLimiting = from.EnableRateLimiting;
        Limit = from.Limit;
        Period = from.Period;
        PeriodTimespan = from.PeriodTimespan;
    }

    /// <summary>The list of allowed clients.</summary>
    /// <value>An <see cref="IList{T}"/> collection of allowed clients.</value>
    public IList<string> ClientWhitelist { get; set; }

    /// <summary>Enables endpoint rate limiting based URL path and HTTP verb.</summary>
    /// <value>A boolean value for enabling endpoint rate limiting based URL path and HTTP verb.</value>
    public bool EnableRateLimiting { get; set; }

    /// <summary>Maximum number of requests that a client can make in a defined period.</summary>
    /// <value>A long integer with maximum number of requests.</value>
    public long Limit { get; set; }

    /// <summary>Rate limit period as in 1s, 1m, 1h, or 1d.</summary>
    /// <value>A string of rate limit period.</value>
    public string Period { get; set; }

    /// <summary>Rate limit period to wait before new request (in seconds).</summary>
    /// <value>A double floating integer with rate limit period.</value>
    public double PeriodTimespan { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        if (!EnableRateLimiting)
        {
            return string.Empty;
        }

        return new StringBuilder()
            .Append($"{nameof(Period)}:{Period},{nameof(PeriodTimespan)}:{PeriodTimespan:F},{nameof(Limit)}:{Limit},{nameof(ClientWhitelist)}:[")
            .AppendJoin(',', ClientWhitelist)
            .Append(']')
            .ToString();
    }
}
