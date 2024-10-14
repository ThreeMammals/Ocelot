namespace Ocelot.Configuration.File
{
    /// <summary>
    /// File model for the "Quality of Service" feature options of the route.
    /// </summary>
    public class FileQoSOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileQoSOptions"/> class.
        /// <para>Default constructor. DON'T CHANGE!..</para>
        /// </summary>
        public FileQoSOptions()
        {
            DurationOfBreak = 1;
            ExceptionsAllowedBeforeBreaking = 0;
            TimeoutValue = null; // default value will be assigned in consumer services: see DownstreamRoute.
        }

        public FileQoSOptions(FileQoSOptions from)
        {
            DurationOfBreak = from.DurationOfBreak;
            ExceptionsAllowedBeforeBreaking = from.ExceptionsAllowedBeforeBreaking;
            TimeoutValue = from.TimeoutValue;
        }

        public FileQoSOptions(QoSOptions from)
        {
            DurationOfBreak = from.DurationOfBreak;
            ExceptionsAllowedBeforeBreaking = from.ExceptionsAllowedBeforeBreaking;
            TimeoutValue = from.TimeoutValue;
        }

        public int DurationOfBreak { get; set; }
        public int ExceptionsAllowedBeforeBreaking { get; set; }

        /// <summary>Explicit timeout value which overrides default one.</summary>
        /// <remarks>Reused in, or ignored in favor of implicit default value:
        /// <list type="bullet">
        ///   <item><see cref="QoSOptions.TimeoutValue"/></item>
        ///   <item><see cref="DownstreamRoute.Timeout"/></item>
        ///   <item><see cref="DownstreamRoute.TimeoutMilliseconds"/></item>
        ///   <item><see cref="DownstreamRoute.DefaultTimeoutSeconds"/></item>
        /// </list>
        /// </remarks>
        /// <value>A <see cref="Nullable{T}"/> (T is <see cref="int"/>) value in milliseconds.</value>
        public int? TimeoutValue { get; set; }
    }
}
