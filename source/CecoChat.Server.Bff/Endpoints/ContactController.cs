using AutoMapper;
using CecoChat.Client.User;
using CecoChat.Contracts.Bff;
using CecoChat.Server.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CecoChat.Server.Bff.Endpoints;

[ApiController]
[Route("api/contact")]
public class ContactController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IUserClient _userClient;

    public ContactController(
        ILogger<ContactController> logger,
        IMapper mapper,
        IUserClient userClient)
    {
        _logger = logger;
        _mapper = mapper;
        _userClient = userClient;
    }

    [Authorize(Policy = "user")]
    [HttpGet]
    [ProducesResponseType(typeof(GetContactsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetContacts(CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        IEnumerable<Contracts.User.Contact> contacts = await _userClient.GetContacts(userClaims.UserId, accessToken, ct);
        GetContactsResponse response = new()
        {
            Contacts = _mapper.Map<Contact[]>(contacts)
        };

        _logger.LogTrace("Responding with {ContactCount} contacts for user {UserId}", response.Contacts.Length, userClaims.UserId);
        return Ok(response);
    }

    // TODO: add logging

    [Authorize(Policy = "user")]
    [HttpPost("{contactId}/invite")]
    [ProducesResponseType(typeof(InviteContactResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> InviteContact([FromRoute(Name = "contactId")] long contactId, [FromBody] InviteContactRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        InviteContactResult result = await _userClient.InviteContact(contactId, userClaims.UserId, accessToken, ct);
        if (result.Success)
        {
            InviteContactResponse response = new()
            {
                Version = result.Version
            };
            return Ok(response);
        }
        if (result.AlreadyExists)
        {
            return Conflict();
        }

        throw new InvalidOperationException($"Failed to process {nameof(InviteContactResult)}.");
    }

    [Authorize(Policy = "user")]
    [HttpPut("{contactId}/invite")]
    [ProducesResponseType(typeof(ApproveContactResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ApproveContact([FromRoute(Name = "contactId")] long contactId, [FromBody] ApproveContactRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        ApproveContactResult result = await _userClient.ApproveContact(contactId, request.Version, userClaims.UserId, accessToken, ct);
        if (result.Success)
        {
            ApproveContactResponse response = new()
            {
                NewVersion = result.NewVersion
            };
            return Ok(response);
        }
        if (result.MissingContact)
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

        throw new InvalidOperationException($"Failed to process {nameof(ApproveContactResult)}.");
    }

    [Authorize(Policy = "user")]
    [HttpDelete("{contactId}/invite")]
    [ProducesResponseType(typeof(CancelContactResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelContact([FromRoute(Name = "contactId")] long contactId, [FromBody] CancelContactRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        CancelContactResult result = await _userClient.CancelContact(contactId, request.Version, userClaims.UserId, accessToken, ct);
        if (result.Success)
        {
            CancelContactResponse response = new();
            return Ok(response);
        }
        if (result.MissingContact)
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

        throw new InvalidOperationException($"Failed to process {nameof(CancelContactResult)}.");
    }

    [Authorize(Policy = "user")]
    [HttpDelete("{contactId}")]
    [ProducesResponseType(typeof(RemoveContactResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveContact([FromRoute(Name = "contactId")] long contactId, [FromBody] RemoveContactRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        RemoveContactResult result = await _userClient.RemoveContact(contactId, request.Version, userClaims.UserId, accessToken, ct);
        if (result.Success)
        {
            RemoveContactResponse response = new();
            return Ok(response);
        }
        if (result.MissingContact)
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

        throw new InvalidOperationException($"Failed to process {nameof(RemoveContactResult)}.");
    }
}
