using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Raft;
using Rafty.Concensus;
using Rafty.FiniteStateMachine;
using Rafty.Infrastructure;
using Rafty.Log;

namespace Ocelot.DependencyInjection
{
    public class OcelotAdministrationBuilder : IOcelotAdministrationBuilder
    {
        private readonly IServiceCollection _services;
        private readonly IConfiguration _configurationRoot;
        
        public OcelotAdministrationBuilder(IServiceCollection services, IConfiguration configurationRoot)
        {
            _configurationRoot = configurationRoot;
            _services = services;    
        }
        
        public IOcelotAdministrationBuilder AddRafty()
        {
            var settings = new InMemorySettings(4000, 5000, 100, 5000);
            _services.AddSingleton<ILog, SqlLiteLog>();
            _services.AddSingleton<IFiniteStateMachine, OcelotFiniteStateMachine>();
            _services.AddSingleton<ISettings>(settings);
            _services.AddSingleton<IPeersProvider, FilePeersProvider>();
            _services.AddSingleton<INode, Node>();
            _services.Configure<FilePeers>(_configurationRoot);
            return this;
        }
    }
}
