using AutoMapper;
using CecoChat.Client.User;
using CecoChat.Contracts.Bff.Connections;
using CecoChat.Server.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CecoChat.Server.Bff.Endpoints.Connections;

[ApiController]
[Route("api/connections")]
[ApiExplorerSettings(GroupName = "Connections")]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class ConnectionController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IConnectionClient _connectionClient;

    public ConnectionController(
        ILogger<ConnectionController> logger,
        IMapper mapper,
        IConnectionClient connectionClient)
    {
        _logger = logger;
        _mapper = mapper;
        _connectionClient = connectionClient;
    }

    [Authorize(Policy = "user")]
    [HttpGet]
    [ProducesResponseType(typeof(GetConnectionsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConnections(CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        IEnumerable<Contracts.User.Connection> connections = await _connectionClient.GetConnections(userClaims.UserId, accessToken, ct);
        GetConnectionsResponse response = new()
        {
            Connections = _mapper.Map<Connection[]>(connections)
        };

        _logger.LogTrace("Responding with {ConnectionCount} connections for user {UserId}", response.Connections.Length, userClaims.UserId);
        return Ok(response);
    }

    // TODO: add logging

    [Authorize(Policy = "user")]
    [HttpPost("{id}/invite")]
    [ProducesResponseType(typeof(InviteConnectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> InviteConnection([FromRoute(Name = "id")] long connectionId, [FromBody] InviteConnectionRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        InviteResult result = await _connectionClient.Invite(connectionId, userClaims.UserId, accessToken, ct);
        if (result.Success)
        {
            InviteConnectionResponse response = new()
            {
                Version = result.Version
            };
            return Ok(response);
        }
        if (result.AlreadyExists)
        {
            return Conflict(new ProblemDetails
            {
                Detail = "Already exists"
            });
        }

        throw new ProcessingFailureException(typeof(InviteResult));
    }

    [Authorize(Policy = "user")]
    [HttpPut("{id}/invite")]
    [ProducesResponseType(typeof(ApproveConnectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ApproveConnection([FromRoute(Name = "id")] long connectionId, [FromBody] ApproveConnectionRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        ApproveResult result = await _connectionClient.Approve(connectionId, request.Version, userClaims.UserId, accessToken, ct);
        if (result.Success)
        {
            ApproveConnectionResponse response = new()
            {
                NewVersion = result.NewVersion
            };
            return Ok(response);
        }
        if (result.MissingConnection)
        {
            return NotFound();
        }
        if (result.Invalid)
        {
            return Conflict(new ProblemDetails
            {
                Detail = "Invalid target ID or status"
            });
        }
        if (result.ConcurrentlyUpdated)
        {
            return Conflict(new ProblemDetails
            {
                Detail = "Concurrently updated"
            });
        }

        throw new ProcessingFailureException(typeof(ApproveResult));
    }

    [Authorize(Policy = "user")]
    [HttpDelete("{id}/invite")]
    [ProducesResponseType(typeof(CancelConnectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelConnection([FromRoute(Name = "id")] long connectionId, [FromBody] CancelConnectionRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        CancelResult result = await _connectionClient.Cancel(connectionId, request.Version, userClaims.UserId, accessToken, ct);
        if (result.Success)
        {
            CancelConnectionResponse response = new();
            return Ok(response);
        }
        if (result.MissingConnection)
        {
            return NotFound();
        }
        if (result.Invalid)
        {
            return Conflict(new ProblemDetails
            {
                Detail = "Invalid status"
            });
        }
        if (result.ConcurrentlyUpdated)
        {
            return Conflict(new ProblemDetails
            {
                Detail = "Concurrently updated"
            });
        }

        throw new ProcessingFailureException(typeof(CancelResult));
    }

    [Authorize(Policy = "user")]
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(RemoveConnectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveConnection([FromRoute(Name = "id")] long connectionId, [FromBody] RemoveConnectionRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        RemoveResult result = await _connectionClient.Remove(connectionId, request.Version, userClaims.UserId, accessToken, ct);
        if (result.Success)
        {
            RemoveConnectionResponse response = new();
            return Ok(response);
        }
        if (result.MissingConnection)
        {
            return NotFound();
        }
        if (result.Invalid)
        {
            return Conflict(new ProblemDetails
            {
                Detail = "Invalid status"
            });
        }
        if (result.ConcurrentlyUpdated)
        {
            return Conflict(new ProblemDetails
            {
                Detail = "Concurrently updated"
            });
        }

        throw new ProcessingFailureException(typeof(RemoveResult));
    }
}
