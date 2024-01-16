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
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicProfile([FromRoute(Name = "id")] long requestedUserId, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        Contracts.User.ProfilePublic? profile = await _profileClient.GetPublicProfile(userClaims.UserId, requestedUserId, accessToken, ct);
        if (profile == null)
        {
            _logger.LogTrace("Responding with missing profile for user {RequestedUserId} requested by user {UserId}", requestedUserId, userClaims.UserId);
            return NotFound();
        }

        GetPublicProfileResponse response = new()
        {
            Profile = _mapper.Map<ProfilePublic>(profile)
        };

        _logger.LogTrace("Responding with profile for user {RequestedUserId} requested by user {UserId}", requestedUserId, userClaims.UserId);
        return Ok(response);
    }

    [Authorize(Policy = "user")]
    [HttpGet(Name = "GetPublicProfiles")]
    [ProducesResponseType(typeof(GetPublicProfilesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicProfiles([FromQuery(Name = "searchPattern")] string? searchPattern, CancellationToken ct)
    {
        // TODO: validate input, and/or consider model binding the request from the query string

        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        string? userIdsString = Request.Query["userIds"].FirstOrDefault();

        IReadOnlyCollection<Contracts.User.ProfilePublic>? profiles = null;
        string source = string.Empty;

        if (!string.IsNullOrWhiteSpace(userIdsString) && string.IsNullOrWhiteSpace(searchPattern))
        {
            if (!TryParseUserIds(userIdsString, out long[]? requestedUserIds))
            {
                return BadRequest(ModelState);
            }

            profiles = await _profileClient.GetPublicProfiles(userClaims.UserId, requestedUserIds, accessToken, ct);
            source = "in the ID list";
        }
        if (!string.IsNullOrWhiteSpace(searchPattern) && string.IsNullOrWhiteSpace(userIdsString))
        {
            profiles = await _profileClient.GetPublicProfiles(userClaims.UserId, searchPattern, accessToken, ct);
            source = "matching the search pattern";
        }

        if (profiles == null)
        {
            ModelState.AddModelError("query", "Either user IDs or a search pattern should be specified.");
            return BadRequest(ModelState);
        }

        GetPublicProfilesResponse response = new()
        {
            Profiles = profiles.Select(profile => _mapper.Map<ProfilePublic>(profile)).ToArray()
        };

        _logger.LogTrace("Responding with {PublicProfileCount} public profiles {PublicProfilesSource} requested by user {UserId}", response.Profiles.Length, source, userClaims.UserId);
        return Ok(response);
    }

    private bool TryParseUserIds(string paramValue, [NotNullWhen(true)] out long[]? userIds)
    {
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
            ModelState.AddModelError("userIds", "Invalid user IDs");
            return false;
        }

        return true;
    }
}
