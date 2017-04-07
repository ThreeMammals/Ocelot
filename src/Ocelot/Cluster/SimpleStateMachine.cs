using System;
using System.Text;
using System.Threading.Tasks;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Setter;
using Ocelot.Logging;
using Rafty.Commands;
using Rafty.State;

namespace Ocelot.Cluster
{
    public class SimpleStateMachine : IStateMachine
    {
        private readonly IFileConfigurationSetter _configSetter;
        private IOcelotLogger _logger;

        public SimpleStateMachine(IFileConfigurationSetter configSetter, IOcelotLoggerFactory loggerFactory)
        {
            _configSetter = configSetter;
            _logger = loggerFactory.CreateLogger<SimpleStateMachine>();
        }
        public async Task Apply(ICommand command)
        {
            var setFileConfig = (SetFileConfiguration)command;

            var response = await _configSetter.Set(setFileConfig.FileConfiguration);
              
            if(response.IsError)
            {
                var builder = new StringBuilder();

                foreach(var error in response.Errors)
                {
                    builder.Append(error);
                }

                _logger.LogDebug(builder.ToString());
            }
        }
    }

    public class SetFileConfiguration : Command
    {
        public FileConfiguration FileConfiguration {get;set;}
    }
}