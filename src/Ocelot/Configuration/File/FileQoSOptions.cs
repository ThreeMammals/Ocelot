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
            TimeoutValue = 0;
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
        public int TimeoutValue { get; set; }
    }
}
