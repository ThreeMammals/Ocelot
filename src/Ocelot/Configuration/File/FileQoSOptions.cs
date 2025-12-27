namespace Ocelot.Configuration.File;

/// <summary>
/// File model for the "Quality of Service" feature options of the route.
/// </summary>
public class FileQoSOptions
{
    /// <summary>Initializes a new instance of the <see cref="FileQoSOptions"/> class.</summary>
    public FileQoSOptions()
    { }

    public FileQoSOptions(FileQoSOptions from)
    {
        DurationOfBreak = from.DurationOfBreak;
        BreakDuration = from.BreakDuration;
        ExceptionsAllowedBeforeBreaking = from.ExceptionsAllowedBeforeBreaking;
        MinimumThroughput = from.MinimumThroughput;
        FailureRatio = from.FailureRatio;
        SamplingDuration = from.SamplingDuration;
        TimeoutValue = from.TimeoutValue;
        Timeout = from.Timeout;
    }

    public FileQoSOptions(QoSOptions from)
    {
        DurationOfBreak = from.BreakDuration;
        BreakDuration = from.BreakDuration;
        ExceptionsAllowedBeforeBreaking = from.MinimumThroughput;
        MinimumThroughput = from.MinimumThroughput;
        FailureRatio = from.FailureRatio;
        SamplingDuration = from.SamplingDuration;
        TimeoutValue = from.Timeout;
        Timeout = from.Timeout;
    }

    [Obsolete("Use BreakDuration instead of DurationOfBreak! Note that DurationOfBreak will be removed in version 25.0!")]
    public int? DurationOfBreak { get; set; }
    public int? BreakDuration { get; set; }

    [Obsolete("Use MinimumThroughput instead of ExceptionsAllowedBeforeBreaking! Note that ExceptionsAllowedBeforeBreaking will be removed in version 25.0!")]
    public int? ExceptionsAllowedBeforeBreaking { get; set; }
    public int? MinimumThroughput { get; set; }

    public double? FailureRatio { get; set; }
    public int? SamplingDuration { get; set; }

    /// <summary>Explicit timeout value which overrides default one.</summary>
    /// <remarks>Reused in, or ignored in favor of implicit default value:
    /// <list type="bullet">
    ///   <item><see cref="QoSOptions.Timeout"/></item>
    ///   <item><see cref="DownstreamRoute.Timeout"/></item>
    ///   <item><see cref="DownstreamRoute.DefaultTimeoutSeconds"/></item>
    /// </list>
    /// </remarks>
    /// <value>A <see cref="Nullable{T}"/> (T is <see cref="int"/>) value in milliseconds.</value>
    [Obsolete("Use Timeout instead of TimeoutValue! Note that TimeoutValue will be removed in version 25.0!")]
    public int? TimeoutValue { get; set; }
    public int? Timeout { get; set; }
}
