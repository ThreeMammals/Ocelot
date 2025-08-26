namespace Ocelot.Configuration.File;

public class FileRateLimitByHeaderRule : FileRateLimitRule
{
    public const string OcClientHeader = "Oc-Client";

    public FileRateLimitByHeaderRule() : base()
    { }

    public FileRateLimitByHeaderRule(FileRateLimitRule from)
        : base(from)
    {
        ClientWhitelist = default;
    }

    public FileRateLimitByHeaderRule(FileRateLimitByHeaderRule from)
        : base(from)
    {
        ClientIdHeader = string.IsNullOrWhiteSpace(from.ClientIdHeader) ? OcClientHeader
            : from.ClientIdHeader;
        ClientWhitelist = from.ClientWhitelist == null ? default
            : new List<string>(from.ClientWhitelist);
    }

    /// <summary>Gets or sets the HTTP header used to store the client identifier, which defaults to <c>Oc-Client</c>.</summary>
    /// <value>A <see cref="string"/> representing the name of the HTTP header.</value>
    public string ClientIdHeader { get; set; } = OcClientHeader;

    /// <summary>A list of approved clients aka whitelisted ones.</summary>
    /// <value>An <see cref="IList{T}"/> collection of allowed clients.</value>
    public IList<string> ClientWhitelist { get; set; }

    /// <summary>
    /// Returns a string that represents the current rule in the format, which defaults to empty string if rate limiting is disabled (<see cref="FileRateLimitRule.EnableRateLimiting"/> is <see langword="false"/>).
    /// </summary>
    /// <remarks>Format: <c>Limit:{limit},Period:{period},PeriodTimespan:{period_timespan},ClientIdHeader:{client_id_header},ClientWhitelist:[{c1,c2,...}]</c>.</remarks>
    /// <returns>A <see cref="string"/> object.</returns>
    public override string ToString() => !EnableRateLimiting ? string.Empty
        : base.ToString() + $",{nameof(ClientIdHeader)}:{ClientIdHeader},{nameof(ClientWhitelist)}:[{string.Join(',', ClientWhitelist ?? [])}]";
}
