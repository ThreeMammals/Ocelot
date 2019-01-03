namespace Ocelot.Provider.Rafty
{
    using System.Threading.Tasks;
    using Configuration.File;
    using Configuration.Setter;
    using global::Rafty.Concensus.Node;
    using global::Rafty.Infrastructure;

    public class RaftyFileConfigurationSetter : IFileConfigurationSetter
    {
        private readonly INode _node;

        public RaftyFileConfigurationSetter(INode node)
        {
            _node = node;
        }

        public async Task<Responses.Response> Set(FileConfiguration fileConfiguration)
        {
            var result = await _node.Accept(new UpdateFileConfiguration(fileConfiguration));

            if (result.GetType() == typeof(ErrorResponse<UpdateFileConfiguration>))
            {
                return new Responses.ErrorResponse(new UnableToSaveAcceptCommand($"unable to save file configuration to state machine"));
            }

            return new Responses.OkResponse();
        }
    }
}
