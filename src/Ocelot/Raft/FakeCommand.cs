using Rafty.FiniteStateMachine;

namespace Ocelot.Raft
{
    public class FakeCommand : ICommand
    {
        public FakeCommand(string value)
        {
            this.Value = value;

        }
        public string Value { get; private set; }
    }
}
