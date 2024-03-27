using AutoMapper;
using CecoChat.Bff.Contracts.Connections;
using CecoChat.Server.Identity;
using CecoChat.User.Client;
using Common;
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

    private static class Details
    {
        public const string MissingUser = "Missing user";
        public const string AlreadyExists = "Already exists";
        public const string ConcurrentlyUpdated = "Concurrently updated";
        public const string InvalidTargetIdOrStatus = "Invalid target ID or status";
        public const string InvalidStatus = "Invalid status";
    }

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

        IEnumerable<User.Contracts.Connection> connections = await _connectionClient.GetConnections(userClaims.UserId, accessToken, ct);
        GetConnectionsResponse response = new()
        {
            Connections = _mapper.Map<Connection[]>(connections)!
        };

        _logger.LogTrace("Responding with {ConnectionCount} connections for user {UserId}", response.Connections.Length, userClaims.UserId);
        return Ok(response);
    }

    [Authorize(Policy = "user")]
    [HttpPost("{id}/invite")]
    [ProducesResponseType(typeof(InviteConnectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
#pragma warning disable IDE0060 // Remove unused parameter
    public async Task<IActionResult> InviteConnection([FromRoute(Name = "id")] long connectionId, [FromBody] InviteConnectionRequest request, CancellationToken ct)
#pragma warning restore IDE0060 // Remove unused parameter
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
            _logger.LogTrace("Responding with a successful invite from {UserId} to {ConnectionId}", userClaims.UserId, connectionId);
            return Ok(response);
        }
        if (result.MissingUser)
        {
            _logger.LogTrace("Responding with a failed invite from {UserId} to {ConnectionId} because the user is missing", userClaims.UserId, connectionId);
            return Conflict(new ProblemDetails
            {
                Detail = Details.MissingUser
            });
        }
        if (result.AlreadyExists)
        {
            _logger.LogTrace("Responding with a failed invite from {UserId} to {ConnectionId} because the connection already exists", userClaims.UserId, connectionId);
            return Conflict(new ProblemDetails
            {
                Detail = Details.AlreadyExists
            });
        }
        if (result.ConcurrentlyUpdated)
        {
            _logger.LogTrace("Responding with a failed invite from {UserId} to {ConnectionId} because the connection has been concurrently updated", userClaims.UserId, connectionId);
            return Conflict(new ProblemDetails
            {
                Detail = Details.ConcurrentlyUpdated
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
            _logger.LogTrace("Responding with a successful approval from {UserId} to {ConnectionId}", userClaims.UserId, connectionId);
            return Ok(response);
        }
        if (result.MissingConnection)
        {
            _logger.LogTrace("Responding with a failed approval from {UserId} to {ConnectionId} because the connection is missing", userClaims.UserId, connectionId);
            return NotFound();
        }
        if (result.Invalid)
        {
            _logger.LogTrace("Responding with a failed approval from {UserId} to {ConnectionId} because of invalid target or status", userClaims.UserId, connectionId);
            return Conflict(new ProblemDetails
            {
                Detail = Details.InvalidTargetIdOrStatus
            });
        }
        if (result.ConcurrentlyUpdated)
        {
            _logger.LogTrace("Responding with a failed approval from {UserId} to {ConnectionId} because the connection has been concurrently updated", userClaims.UserId, connectionId);
            return Conflict(new ProblemDetails
            {
                Detail = Details.ConcurrentlyUpdated
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
            CancelConnectionResponse response = new()
            {
                NewVersion = result.NewVersion
            };

            _logger.LogTrace("Responding with a successful cancel from {UserId} to {ConnectionId}", userClaims.UserId, connectionId);
            return Ok(response);
        }
        if (result.MissingConnection)
        {
            _logger.LogTrace("Responding with a failed cancel from {UserId} to {ConnectionId} because the connection is missing", userClaims.UserId, connectionId);
            return NotFound();
        }
        if (result.Invalid)
        {
            _logger.LogTrace("Responding with a failed cancel from {UserId} to {ConnectionId} because of invalid status", userClaims.UserId, connectionId);
            return Conflict(new ProblemDetails
            {
                Detail = Details.InvalidStatus
            });
        }
        if (result.ConcurrentlyUpdated)
        {
            _logger.LogTrace("Responding with a failed cancel from {UserId} to {ConnectionId} because the connection has been concurrently updated", userClaims.UserId, connectionId);
            return Conflict(new ProblemDetails
            {
                Detail = Details.ConcurrentlyUpdated
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
            RemoveConnectionResponse response = new()
            {
                NewVersion = result.NewVersion
            };

            _logger.LogTrace("Responding with a successful removal from {UserId} to {ConnectionId}", userClaims.UserId, connectionId);
            return Ok(response);
        }
        if (result.MissingConnection)
        {
            _logger.LogTrace("Responding with a failed removal from {UserId} to {ConnectionId} because the connection is missing", userClaims.UserId, connectionId);
            return NotFound();
        }
        if (result.Invalid)
        {
            _logger.LogTrace("Responding with a failed removal from {UserId} to {ConnectionId} because of invalid status", userClaims.UserId, connectionId);
            return Conflict(new ProblemDetails
            {
                Detail = Details.InvalidStatus
            });
        }
        if (result.ConcurrentlyUpdated)
        {
            _logger.LogTrace("Responding with a failed removal from {UserId} to {ConnectionId} because the connection has been concurrently updated", userClaims.UserId, connectionId);
            return Conflict(new ProblemDetails
            {
                Detail = Details.ConcurrentlyUpdated
            });
        }

        throw new ProcessingFailureException(typeof(RemoveResult));
    }
}
