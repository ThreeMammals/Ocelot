using Ocelot.Configuration.File;
using Rafty.FiniteStateMachine;

namespace Ocelot.Raft
{
    public class UpdateFileConfiguration : ICommand
    {
        public UpdateFileConfiguration(FileConfiguration configuration)
        {
            Configuration = configuration;
        }
        
        public FileConfiguration Configuration {get;private set;}
    }
}