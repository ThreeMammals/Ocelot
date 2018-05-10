using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rafty.FiniteStateMachine;
using Rafty.Infrastructure;
using Rafty.Log;

namespace Ocelot.Raft
{    
    [ExcludeFromCoverage]
    public class FileFsm : IFiniteStateMachine
    {
        private string _id;

        public FileFsm(NodeId nodeId)
        {
            _id = nodeId.Id.Replace("/","").Replace(":","");
        }
        
        public Task Handle(LogEntry log)
        {
            try
            {
                var json = JsonConvert.SerializeObject(log.CommandData);
                File.AppendAllText(_id, json);
            }
            catch(Exception exception)
            {
                Console.WriteLine(exception);
            }

            return Task.CompletedTask;
        }
    }
}
