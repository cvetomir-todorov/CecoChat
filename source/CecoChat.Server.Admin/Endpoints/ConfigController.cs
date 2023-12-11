using CecoChat.Contracts.Admin;
using CecoChat.Data.Config;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CecoChat.Server.Admin.Endpoints;

[ApiController]
[Route("api/config")]
[ApiExplorerSettings(GroupName = "Config")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class ConfigController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly ConfigDbContext _configDbContext;

    public ConfigController(
        ILogger<ConfigController> logger,
        ConfigDbContext configDbContext)
    {
        _logger = logger;
        _configDbContext = configDbContext;
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetConfigResponse), StatusCodes.Status200OK)]
#pragma warning disable IDE0060 // Remove unused parameter
    public async Task<IActionResult> GetConfig([FromQuery] GetConfigRequest request, CancellationToken ct)
#pragma warning restore IDE0060 // Remove unused parameter
    {
        List<ElementEntity> entities = await _configDbContext.Elements.AsNoTracking().ToListAsync(ct);
        _logger.LogInformation("Received {ConfigElementCount} config elements", entities.Count);

        ConfigElement[] elements = new ConfigElement[entities.Count];
        int index = 0;

        foreach (ElementEntity entity in entities)
        {
            elements[index] = new ConfigElement
            {
                Name = entity.Name,
                Value = entity.Value,
                Version = entity.Version
            };

            index++;
        }

        _logger.LogInformation("Returning {ConfigElementCount} config elements", entities.Count);
        return Ok(new GetConfigResponse
        {
            Elements = elements
        });
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateConfigElements([FromBody] [BindRequired] UpdateConfigElementsRequest request, CancellationToken ct)
    {
        DateTime newVersion = DateTime.UtcNow;

        foreach (ConfigElement element in request.Elements)
        {
            ElementEntity entity = new()
            {
                Name = element.Name,
                Value = element.Value,
                Version = newVersion
            };

            EntityEntry<ElementEntity> entry = _configDbContext.Attach(entity);
            entry.Property(e => e.Version).OriginalValue = element.Version;
            entry.Property(e => e.Value).IsModified = true;
        }

        try
        {
            await _configDbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict();
        }

        return Ok();
    }
}
