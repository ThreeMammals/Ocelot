using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Configuration.Setter;
using Ocelot.Infrastructure.Extensions;

namespace Ocelot.Administration;

// [ApiController] // TODO: Make it ApiController
[Authorize]
[Route("configuration")]
public class FileConfigurationController : Controller
{
    private readonly IFileConfigurationRepository _repo;
    private readonly IFileConfigurationSetter _setter;

    public FileConfigurationController(IFileConfigurationRepository repo, IFileConfigurationSetter setter)
    {
        _repo = repo;
        _setter = setter;
    }

    [HttpGet]
    public async Task<ActionResult> Get()
    {
        try
        {
            var fileConfiguration = await _repo.GetAsync();
            return Ok(fileConfiguration);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.GetMessages());
        }
    }

    [HttpPost]
    public async Task<ActionResult> Post([FromBody] FileConfiguration fileConfiguration)
    {
        try
        {
            var response = await _setter.Set(fileConfiguration);
            if (response.IsError)
            {
                return BadRequest(response.Errors);
            }

            return Ok(fileConfiguration);
        }
        catch (Exception e)
        {
            return BadRequest($"{e.Message}:{Environment.NewLine}{e.StackTrace}");
        }
    }
}
