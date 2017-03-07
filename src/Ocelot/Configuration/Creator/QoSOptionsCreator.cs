using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public class QoSOptionsCreator : IQoSOptionsCreator
    {
        public QoSOptions Create(FileReRoute fileReRoute)
        {
            return new QoSOptionsBuilder()
                .WithExceptionsAllowedBeforeBreaking(fileReRoute.QoSOptions.ExceptionsAllowedBeforeBreaking)
                .WithDurationOfBreak(fileReRoute.QoSOptions.DurationOfBreak)
                .WithTimeoutValue(fileReRoute.QoSOptions.TimeoutValue)
                .Build();
        }
    }
}