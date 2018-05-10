using System.Threading.Tasks;
using Ocelot.Configuration.Setter;
using Rafty.FiniteStateMachine;
using Rafty.Log;

namespace Ocelot.Raft
{
    [ExcludeFromCoverage]
    public class OcelotFiniteStateMachine : IFiniteStateMachine
    {
        private IFileConfigurationSetter _setter;

        public OcelotFiniteStateMachine(IFileConfigurationSetter setter)
        {
            _setter = setter;
        }

        public async Task Handle(LogEntry log)
        {
            //todo - handle an error
            //hack it to just cast as at the moment we know this is the only command :P
            var hack = (UpdateFileConfiguration)log.CommandData;
            await _setter.Set(hack.Configuration);
        }
    }
}
