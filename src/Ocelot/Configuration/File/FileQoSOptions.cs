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
        ExceptionsAllowedBeforeBreaking = from.ExceptionsAllowedBeforeBreaking;
        FailureRatio = from.FailureRatio;
        SamplingDuration = from.SamplingDuration;
        TimeoutValue = from.TimeoutValue;
    }

    public FileQoSOptions(QoSOptions from)
    {
        DurationOfBreak = from.BreakDuration;
        ExceptionsAllowedBeforeBreaking = from.MinimumThroughput;
        FailureRatio = from.FailureRatio;
        SamplingDuration = from.SamplingDuration;
        TimeoutValue = from.Timeout;
    }

    public int? DurationOfBreak { get; set; }
    public int? ExceptionsAllowedBeforeBreaking { get; set; }
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
    public int? TimeoutValue { get; set; }
}
