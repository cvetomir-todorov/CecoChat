using CecoChat.Contracts.Admin;
using CecoChat.Data.Config;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Npgsql;

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
        UpdateDbContext(request.ExistingElements, request.NewElements);
        IActionResult result = await ExecuteUpdates(ct);

        return result;
    }

    private void UpdateDbContext(IReadOnlyCollection<ConfigElement> existingElements, IReadOnlyCollection<ConfigElement> newElements)
    {
        DateTime newVersion = DateTime.UtcNow;

        foreach (ConfigElement existingElement in existingElements)
        {
            ElementEntity entity = Map(existingElement, newVersion);
            EntityEntry<ElementEntity> entry = _configDbContext.Attach(entity);
            entry.Property(e => e.Version).OriginalValue = existingElement.Version;
            entry.Property(e => e.Value).IsModified = true;
        }

        foreach (ConfigElement newElement in newElements)
        {
            ElementEntity entity = Map(newElement, newVersion);
            _configDbContext.Add(entity);
        }
    }

    private static ElementEntity Map(ConfigElement element, DateTime newVersion)
    {
        return new()
        {
            Name = element.Name,
            Value = element.Value,
            Version = newVersion
        };
    }

    private async Task<IActionResult> ExecuteUpdates(CancellationToken ct)
    {
        try
        {
            await _configDbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Successfully updated config");
            return Ok();
        }
        catch (DbUpdateException dbUpdateException)
            when (dbUpdateException.InnerException is PostgresException postgresException &&
                  postgresException.SqlState == "23505" &&
                  postgresException.MessageText.Contains("Elements_pkey"))
        {
            _logger.LogError(dbUpdateException, "Failed to update config because some or all of the newly-added elements have keys that are already present");
            return Conflict(new ProblemDetails
            {
                Detail = "New elements keys are already present"
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogError("Failed to update config because some or all of the elements have been concurrently updated");
            return Conflict(new ProblemDetails
            {
                Detail = "Existing elements have been concurrently updated"
            });
        }
    }
}