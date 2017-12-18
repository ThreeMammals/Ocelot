using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Provider;
using Ocelot.Configuration.Setter;
using Ocelot.Raft;
using Rafty.Concensus;

namespace Ocelot.Controllers
{
    [Authorize]
    [Route("configuration")]
    public class FileConfigurationController : Controller
    {
        private readonly IFileConfigurationProvider _configGetter;
        private readonly IFileConfigurationSetter _configSetter;
        private readonly INode _node;

        public FileConfigurationController(IFileConfigurationProvider getFileConfig, IFileConfigurationSetter configSetter, INode node)
        {
            _node = node;
            _configGetter = getFileConfig;
            _configSetter = configSetter;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var response = await _configGetter.Get();

            if(response.IsError)
            {
                return new BadRequestObjectResult(response.Errors);
            }

            return new OkObjectResult(response.Data);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody]FileConfiguration fileConfiguration)
        {
            _node.Accept(new UpdateFileConfiguration(fileConfiguration));
            // var response = await _configSetter.Set(fileConfiguration);
              
            // if(response.IsError)
            // {
            //     return new BadRequestObjectResult(response.Errors);
            // }

            return new OkObjectResult(fileConfiguration);
        }
    }
}