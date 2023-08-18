using AutoMapper;
using CecoChat.Client.User;
using CecoChat.Contracts.Bff;
using CecoChat.Server.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CecoChat.Server.Bff.Endpoints;

[ApiController]
[Route("api/connection")]
public class ConnectionController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IUserClient _userClient;

    public ConnectionController(
        ILogger<ConnectionController> logger,
        IMapper mapper,
        IUserClient userClient)
    {
        _logger = logger;
        _mapper = mapper;
        _userClient = userClient;
    }

    [Authorize(Policy = "user")]
    [HttpGet]
    [ProducesResponseType(typeof(GetConnectionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetConnections(CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        IEnumerable<Contracts.User.Connection> connections = await _userClient.GetConnections(userClaims.UserId, accessToken, ct);
        GetConnectionsResponse response = new()
        {
            Connections = _mapper.Map<Connection[]>(connections)
        };

        _logger.LogTrace("Responding with {ConnectionCount} connections for user {UserId}", response.Connections.Length, userClaims.UserId);
        return Ok(response);
    }

    // TODO: add logging

    [Authorize(Policy = "user")]
    [HttpPost("{connectionId}/invite")]
    [ProducesResponseType(typeof(InviteConnectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> InviteConnection([FromRoute(Name = "connectionId")] long connectionId, [FromBody] InviteConnectionRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        InviteResult result = await _userClient.Invite(connectionId, userClaims.UserId, accessToken, ct);
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
            return Conflict();
        }

        throw new ProcessingFailureException(typeof(InviteResult));
    }

    [Authorize(Policy = "user")]
    [HttpPut("{connectionId}/invite")]
    [ProducesResponseType(typeof(ApproveConnectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ApproveConnection([FromRoute(Name = "connectionId")] long connectionId, [FromBody] ApproveConnectionRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        ApproveResult result = await _userClient.Approve(connectionId, request.Version, userClaims.UserId, accessToken, ct);
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
            return Conflict("invalid");
        }
        if (result.ConcurrentlyUpdated)
        {
            return Conflict("concurrently updated");
        }

        throw new ProcessingFailureException(typeof(ApproveResult));
    }

    [Authorize(Policy = "user")]
    [HttpDelete("{connectionId}/invite")]
    [ProducesResponseType(typeof(CancelConnectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelConnection([FromRoute(Name = "connectionId")] long connectionId, [FromBody] CancelConnectionRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        CancelResult result = await _userClient.Cancel(connectionId, request.Version, userClaims.UserId, accessToken, ct);
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
            return Conflict("invalid");
        }
        if (result.ConcurrentlyUpdated)
        {
            return Conflict("concurrently updated");
        }

        throw new ProcessingFailureException(typeof(CancelResult));
    }

    [Authorize(Policy = "user")]
    [HttpDelete("{connectionId}")]
    [ProducesResponseType(typeof(RemoveConnectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveConnection([FromRoute(Name = "connectionId")] long connectionId, [FromBody] RemoveConnectionRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        RemoveResult result = await _userClient.Remove(connectionId, request.Version, userClaims.UserId, accessToken, ct);
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
            return Conflict("invalid");
        }
        if (result.ConcurrentlyUpdated)
        {
            return Conflict("concurrently updated");
        }

        throw new ProcessingFailureException(typeof(RemoveResult));
    }
}
