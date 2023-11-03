namespace Ocelot.Configuration.File
{
    public class FileQoSOptions
    {
        public FileQoSOptions()
        {
            DurationOfBreak = 1;
            ExceptionsAllowedBeforeBreaking = 0;
            TimeoutValue = int.MaxValue;
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
