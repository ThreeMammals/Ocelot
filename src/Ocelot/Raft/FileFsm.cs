using System;
using System.IO;
using Newtonsoft.Json;
using Rafty.FiniteStateMachine;
using Rafty.Infrastructure;
using Rafty.Log;

namespace Ocelot.Raft
{
    public class FileFsm : IFiniteStateMachine
    {
        private Guid _id;
        public FileFsm(NodeId nodeId)
        {
            _id = nodeId.Id;
        }
        
        public void Handle(LogEntry log)
        {
            try
            {
                var json = JsonConvert.SerializeObject(log.CommandData);
                File.AppendAllText(_id.ToString(), json);
            }
            catch(Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
    }
}
