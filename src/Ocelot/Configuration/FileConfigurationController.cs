using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Setter;
using Ocelot.Raft;
using Rafty.Concensus;

namespace Ocelot.Configuration
{
    using Repository;

    [Authorize]
    [Route("configuration")]
    public class FileConfigurationController : Controller
    {
        private readonly IFileConfigurationRepository _repo;
        private readonly IFileConfigurationSetter _setter;
        private readonly IServiceProvider _provider;

        public FileConfigurationController(IFileConfigurationRepository repo, IFileConfigurationSetter setter, IServiceProvider provider)
        {
            _repo = repo;
            _setter = setter;
            _provider = provider;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var response = await _repo.Get();

            if(response.IsError)
            {
                return new BadRequestObjectResult(response.Errors);
            }

            return new OkObjectResult(response.Data);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody]FileConfiguration fileConfiguration)
        {
            try
            {
                //todo - this code is a bit shit sort it out..
                var test = _provider.GetService(typeof(INode));
                if (test != null)
                {
                    var node = (INode)test;
                    var result = await node.Accept(new UpdateFileConfiguration(fileConfiguration));
                    if (result.GetType() == typeof(Rafty.Concensus.ErrorResponse<UpdateFileConfiguration>))
                    {
                        return new BadRequestObjectResult("There was a problem. This error message sucks raise an issue in GitHub.");
                    }

                    return new OkObjectResult(result.Command.Configuration);
                }

                var response = await _setter.Set(fileConfiguration);

                if (response.IsError)
                {
                    return new BadRequestObjectResult(response.Errors);
                }

                return new OkObjectResult(fileConfiguration);
            }
            catch(Exception e)
            {
                return new BadRequestObjectResult($"{e.Message}:{e.StackTrace}");
            }
        }
    }
}
