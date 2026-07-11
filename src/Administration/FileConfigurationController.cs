using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Errors;

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
    public async Task<ActionResult> Get(CancellationToken cancellationToken)
    {
        FileConfiguration configuration;
        try
        {
            configuration = await _repo.GetAsync(cancellationToken);
        }
        catch (Exception e)
        {
            return BadRequest(e.ToShortString());
        }

        return (configuration is null)
            ? BadRequest($"The {_repo.GetType().Name} has returned nothing")
            : Ok(configuration);
    }

    [HttpPost]
    public async Task<ActionResult> Post([FromBody] FileConfiguration configuration, CancellationToken cancellationToken)
    {
        try
        {
            await _setter.SetAsync(configuration, cancellationToken);
            return Ok(configuration);
        }
        catch (Exception e)
        {
            return BadRequest(e.ToShortString());
        }
    }
}
