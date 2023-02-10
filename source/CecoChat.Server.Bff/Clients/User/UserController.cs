using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using CecoChat.Client.User;
using CecoChat.Contracts.Bff;
using CecoChat.Server.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CecoChat.Server.Bff.Clients.User;

[ApiController]
[Route("api")]
public class UserController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IUserClient _userClient;

    public UserController(
        ILogger<UserController> logger,
        IUserClient userClient)
    {
        _logger = logger;
        _userClient = userClient;
    }

    [Authorize(Roles = "user")]
    [HttpGet("user/profile/{id}", Name = "GetPublicProfile")]
    public async Task<IActionResult> GetPublicProfile([FromRoute(Name = "id")] long requestedUserId, CancellationToken ct)
    {
        if (!TryGetUserClaims(HttpContext, out UserClaims? userClaims))
        {
            return Unauthorized();
        }
        if (!HttpContext.TryGetBearerAccessTokenValue(out string? accessToken))
        {
            return Unauthorized();
        }

        Contracts.User.ProfilePublic profile = await _userClient.GetPublicProfile(userClaims.UserId, requestedUserId, accessToken, ct);
        GetPublicProfileResponse response = new() { Profile = MapPublicProfile(profile) };

        _logger.LogTrace("Responding with profile for user {RequestedUserId} requested by user {UserId}", requestedUserId, userClaims.UserId);
        return Ok(response);
    }

    [Authorize(Roles = "user")]
    [HttpGet("user/profile", Name = "GetPublicProfiles")]
    public async Task<IActionResult> GetPublicProfiles(CancellationToken ct)
    {
        if (!TryGetUserClaims(HttpContext, out UserClaims? userClaims))
        {
            return Unauthorized();
        }
        if (!HttpContext.TryGetBearerAccessTokenValue(out string? accessToken))
        {
            return Unauthorized();
        }

        if (!TryParseUserIds("user_ids", out long[]? requestedUserIds))
        {
            return BadRequest(ModelState);
        }

        IEnumerable<Contracts.User.ProfilePublic> profiles = await _userClient.GetPublicProfiles(userClaims.UserId, requestedUserIds, accessToken, ct);
        GetPublicProfilesResponse response = new();
        response.Profiles = profiles.Select(MapPublicProfile).ToArray();

        _logger.LogTrace("Responding with {PublicProfileCount} public profiles requested by user {UserId}", response.Profiles.Length, userClaims.UserId);
        return Ok(response);
    }

    private bool TryGetUserClaims(HttpContext context, [NotNullWhen(true)] out UserClaims? userClaims)
    {
        if (!context.User.TryGetUserClaims(out userClaims))
        {
            _logger.LogError("Client from was authorized but has no parseable access token");
            return false;
        }

        Activity.Current?.SetTag("user.id", userClaims.UserId);
        return true;
    }

    private static ProfilePublic MapPublicProfile(Contracts.User.ProfilePublic profile)
    {
        return new ProfilePublic
        {
            UserId = profile.UserId,
            UserName = profile.UserName,
            DisplayName = profile.DisplayName,
            AvatarUrl = profile.AvatarUrl
        };
    }

    // TODO: consider creating an MVC value provider and a factory for it
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
