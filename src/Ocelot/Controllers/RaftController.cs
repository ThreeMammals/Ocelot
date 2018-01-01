using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Ocelot.Logging;
using Ocelot.Raft;
using Rafty.Concensus;
using Rafty.FiniteStateMachine;

namespace Ocelot.Controllers
{
    [Authorize]
    [Route("raft")]
    public class RaftController : Controller
    {
        private readonly INode _node;
        private IOcelotLogger _logger;
        private string _baseSchemeUrlAndPort;

        public RaftController(INode node, IOcelotLoggerFactory loggerFactory, IWebHostBuilder builder)
        {
            _baseSchemeUrlAndPort = builder.GetSetting(WebHostDefaults.ServerUrlsKey);
            _logger = loggerFactory.CreateLogger<RaftController>();
            _node = node;
        }

        [Route("appendentries")]
        public async Task<IActionResult> AppendEntries()
        {
             var reader = new StreamReader(HttpContext.Request.Body);
                var json = await reader.ReadToEndAsync();
                var appendEntries = JsonConvert.DeserializeObject<AppendEntries>(json, new JsonSerializerSettings {
                    TypeNameHandling = TypeNameHandling.All
                });
            _logger.LogDebug($"{_baseSchemeUrlAndPort}/appendentries called, my state is {_node.State.GetType().FullName}");
            var appendEntriesResponse = _node.Handle(appendEntries);
            return new OkObjectResult(appendEntriesResponse);
        }

        [Route("requestvote")]
        public async Task<IActionResult> RequestVote()
        { 
               var reader = new StreamReader(HttpContext.Request.Body);
                var json = await reader.ReadToEndAsync();
                var requestVote = JsonConvert.DeserializeObject<RequestVote>(json, new JsonSerializerSettings {
                    TypeNameHandling = TypeNameHandling.All
                });
            _logger.LogDebug($"{_baseSchemeUrlAndPort}/requestvote called, my state is {_node.State.GetType().FullName}");
            var requestVoteResponse = _node.Handle(requestVote);
            return new OkObjectResult(requestVoteResponse);
        }

        [Route("command")]
        public async Task<IActionResult> Command()
        { 
            try
            {
                var reader = new StreamReader(HttpContext.Request.Body);
                var json = await reader.ReadToEndAsync();
                var command = JsonConvert.DeserializeObject<ICommand>(json, new JsonSerializerSettings {
                    TypeNameHandling = TypeNameHandling.All
                });
                _logger.LogDebug($"{_baseSchemeUrlAndPort}/command called, my state is {_node.State.GetType().FullName}");
                var commandResponse = _node.Accept(command);
                return new OkObjectResult(commandResponse);
            }
            catch(Exception e)
            {
                _logger.LogError("THERE WAS A PROBLEM", e);
                throw e;
            }
        }
    }
}