using CecoChat.Config.Data;
using CecoChat.DynamicConfig.Backplane;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Npgsql;

namespace CecoChat.Server.Config.Endpoints;

[ApiController]
[Route("api/config")]
[ApiExplorerSettings(GroupName = "Config")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class ConfigController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly ConfigDbContext _configDbContext;
    private readonly IConfigChangesProducer _configChangesProducer;

    public ConfigController(
        ILogger<ConfigController> logger,
        ConfigDbContext configDbContext,
        IConfigChangesProducer configChangesProducer)
    {
        _logger = logger;
        _configDbContext = configDbContext;
        _configChangesProducer = configChangesProducer;
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
    public async Task<IActionResult> UpdateConfigElements([FromBody][BindRequired] UpdateConfigElementsRequest request, CancellationToken ct)
    {
        UpdateDbContext(request.ExistingElements, request.NewElements, request.DeletedElements);
        (bool success, IActionResult result) = await ExecuteUpdates(ct);

        if (success)
        {
            NotifyChanges(request.ExistingElements.Union(request.NewElements).Union(request.DeletedElements));
        }

        return result;
    }

    private void UpdateDbContext(
        IReadOnlyCollection<ConfigElement> existingElements,
        IReadOnlyCollection<ConfigElement> newElements,
        IReadOnlyCollection<ConfigElement> deletedElements)
    {
        DateTime newVersion = DateTime.UtcNow;

        foreach (ConfigElement existingElement in existingElements)
        {
            ElementEntity entity = new()
            {
                Name = existingElement.Name,
                Value = existingElement.Value,
                Version = newVersion
            };

            EntityEntry<ElementEntity> entry = _configDbContext.Attach(entity);
            entry.Property(e => e.Version).OriginalValue = existingElement.Version;
            entry.Property(e => e.Value).IsModified = true;
        }
        foreach (ConfigElement newElement in newElements)
        {
            ElementEntity entity = new()
            {
                Name = newElement.Name,
                Value = newElement.Value,
                Version = newVersion
            };

            _configDbContext.Add(entity);
        }
        foreach (ConfigElement deletedElement in deletedElements)
        {
            ElementEntity entity = new()
            {
                Name = deletedElement.Name,
                Version = deletedElement.Version
            };

            _configDbContext.Remove(entity);
        }
    }

    private async Task<(bool, IActionResult)> ExecuteUpdates(CancellationToken ct)
    {
        try
        {
            await _configDbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Successfully updated config");
            return (true, Ok());
        }
        catch (DbUpdateException dbUpdateException)
            when (dbUpdateException.InnerException is PostgresException postgresException &&
                  postgresException.SqlState == "23505" &&
                  postgresException.MessageText.Contains("elements_pkey"))
        {
            _logger.LogError(dbUpdateException, "Failed to update config because some or all of the newly-added elements have keys that are already present");
            ConflictObjectResult conflict = Conflict(new ProblemDetails
            {
                Detail = "New elements keys are already present"
            });

            return (false, conflict);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogError("Failed to update config because some or all of the elements have been concurrently updated");
            ConflictObjectResult conflict = Conflict(new ProblemDetails
            {
                Detail = "Existing elements have been concurrently updated"
            });

            return (false, conflict);
        }
    }

    private void NotifyChanges(IEnumerable<ConfigElement> allElements)
    {
        List<string> configSections = allElements
            .Select(element => element.Name)
            .Select(elementName =>
            {
                int dotIndex = elementName.IndexOf('.');
                if (dotIndex <= 0)
                {
                    _logger.LogWarning("Config element name {ConfigElementName} doesn't have a section name", elementName);
                    return string.Empty;
                }

                return elementName.Substring(0, dotIndex);
            })
            .Where(configSection => configSection.Length > 0)
            .Distinct()
            .ToList();

        _logger.LogInformation("Changes in config sections: {ChangedConfigSections}", string.Join(separator: ',', configSections));
        foreach (string configSection in configSections)
        {
            _configChangesProducer.NotifyChanges(configSection);
        }
    }
}
