using System.Diagnostics.CodeAnalysis;
using AutoMapper;
using CecoChat.Client.User;
using CecoChat.Contracts.Bff.Profiles;
using CecoChat.Server.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CecoChat.Server.Bff.Endpoints.Profiles;

[ApiController]
[Route("api/profiles")]
[ApiExplorerSettings(GroupName = "Profiles")]
public class ProfileController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IProfileClient _profileClient;

    public ProfileController(
        ILogger<ProfileController> logger,
        IMapper mapper,
        IProfileClient profileClient)
    {
        _logger = logger;
        _mapper = mapper;
        _profileClient = profileClient;
    }

    [Authorize(Policy = "user")]
    [HttpGet("{id}", Name = "GetPublicProfile")]
    [ProducesResponseType(typeof(GetPublicProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPublicProfile([FromRoute(Name = "id")] long requestedUserId, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        Contracts.User.ProfilePublic profile = await _profileClient.GetPublicProfile(userClaims.UserId, requestedUserId, accessToken, ct);
        GetPublicProfileResponse response = new() { Profile = _mapper.Map<ProfilePublic>(profile) };

        _logger.LogTrace("Responding with profile for user {RequestedUserId} requested by user {UserId}", requestedUserId, userClaims.UserId);
        return Ok(response);
    }

    [Authorize(Policy = "user")]
    [HttpGet(Name = "GetPublicProfiles")]
    [ProducesResponseType(typeof(GetPublicProfilesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPublicProfiles(CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }
        if (!TryParseUserIds("userIds", out long[]? requestedUserIds))
        {
            return BadRequest(ModelState);
        }

        IEnumerable<Contracts.User.ProfilePublic> profiles = await _profileClient.GetPublicProfiles(userClaims.UserId, requestedUserIds, accessToken, ct);
        GetPublicProfilesResponse response = new()
        {
            Profiles = profiles.Select(profile => _mapper.Map<ProfilePublic>(profile)).ToArray()
        };

        _logger.LogTrace("Responding with {PublicProfileCount} public profiles requested by user {UserId}", response.Profiles.Length, userClaims.UserId);
        return Ok(response);
    }

    private bool TryParseUserIds(string paramName, [NotNullWhen(true)] out long[]? userIds)
    {
        string? paramValue = Request.Query[paramName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(paramValue))
        {
            ModelState.AddModelError(nameof(paramName), "Missing user IDs");
            userIds = null;
            return false;
        }

        string[] userIdStrings = paramValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        userIds = new long[userIdStrings.Length];
        int invalidCount = 0;

        for (int i = 0; i < userIdStrings.Length; ++i)
        {
            if (!long.TryParse(userIdStrings[i], out long userId))
            {
                invalidCount++;
            }
            else
            {
                userIds[i] = userId;
            }
        }

        if (invalidCount > 0)
        {
            ModelState.AddModelError(nameof(paramName), "Invalid user IDs");
            return false;
        }

        return true;
    }
}
