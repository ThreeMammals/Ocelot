using Moq;
using Ocelot.Cluster;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Setter;
using Ocelot.Logging;
using Ocelot.Responses;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Cluster
{
    public class SimpleStateMachineTests
    {
        private SimpleStateMachine _stateMachine;
        private Mock<IFileConfigurationSetter> _configSetter;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private SetFileConfiguration _command;

        public SimpleStateMachineTests()
        {
            _configSetter = new Mock<IFileConfigurationSetter>();
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _stateMachine = new SimpleStateMachine(_configSetter.Object, _loggerFactory.Object);
        }

        [Fact]
        public void should_persist_command_to_state_machine()
        {
            this.Given(x => GivenTheFollowingCommand(new SetFileConfiguration()))
                .When(x => WhenTheCommandIsApplied())
                .Then(x => ThenTheStateMachineCallsDependenciesCorrectly())
                .BDDfy();
        }

        private void GivenTheFollowingCommand(SetFileConfiguration setFileConfiguration)
        {
            _command = setFileConfiguration;
            _configSetter
                .Setup(x => x.Set(It.IsAny<FileConfiguration>()))
                .ReturnsAsync(new OkResponse());
        }

        private void WhenTheCommandIsApplied()
        {
            _stateMachine.Apply(_command).Wait();
        }

        private void ThenTheStateMachineCallsDependenciesCorrectly()
        {
            _configSetter.Verify(x => x.Set(_command.FileConfiguration), Times.Once);
        }
    }
}