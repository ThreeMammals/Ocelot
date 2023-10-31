namespace Ocelot.Configuration.File
{
    public class FileQoSOptions
    {
        public int ExceptionsAllowedBeforeBreaking { get; set; }

        public int DurationOfBreak { get; set; }

        public int TimeoutValue { get; set; }

        public int RetryCount { get; set; }
        public int RetryNumber { get; set; }
    }
}
