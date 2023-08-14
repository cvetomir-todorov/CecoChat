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
        if (!HttpContext.TryGetUserClaims(_logger, out UserClaims? userClaims))
        {
            return Unauthorized();
        }
        if (!HttpContext.TryGetBearerAccessTokenValue(out string? accessToken))
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
}
