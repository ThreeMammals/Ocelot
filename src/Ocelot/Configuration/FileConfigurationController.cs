using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Provider;
using Ocelot.Configuration.Setter;
using Ocelot.Raft;
using Rafty.Concensus;

namespace Ocelot.Configuration
{
    [Authorize]
    [Route("configuration")]
    public class FileConfigurationController : Controller
    {
        private readonly IFileConfigurationProvider _configGetter;
        private readonly IFileConfigurationSetter _configSetter;
        private readonly IServiceProvider _serviceProvider;

        public FileConfigurationController(IFileConfigurationProvider getFileConfig, IFileConfigurationSetter configSetter, IServiceProvider serviceProvider)
        {
            _configGetter = getFileConfig;
            _configSetter = configSetter;
            _serviceProvider = serviceProvider;
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
            //todo - this code is a bit shit sort it out..
            var test = _serviceProvider.GetService(typeof(INode));
            if (test != null)
            {
                var node = (INode)test;
                var result = node.Accept(new UpdateFileConfiguration(fileConfiguration));
                if (result.GetType() == typeof(Rafty.Concensus.ErrorResponse<UpdateFileConfiguration>))
                {
                    return new BadRequestObjectResult("There was a problem. This error message sucks raise an issue in GitHub.");
                }

                return new OkObjectResult(result.Command.Configuration);
            }

            var response = await _configSetter.Set(fileConfiguration);

            if (response.IsError)
            {
                return new BadRequestObjectResult(response.Errors);
            }

            return new OkObjectResult(fileConfiguration);
        }
    }
}
