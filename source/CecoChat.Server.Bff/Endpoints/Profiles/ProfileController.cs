using AutoMapper;
using CecoChat.AspNet.ModelBinding;
using CecoChat.Client.User;
using CecoChat.Contracts.Bff.Profiles;
using CecoChat.Server.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CecoChat.Server.Bff.Endpoints.Profiles;

public sealed class GetPublicProfilesRequest
{
    [FromQuery(Name = "userIds")]
    [ModelBinder(BinderType = typeof(LongArrayCsvModelBinder))]
    public long[] UserIds { get; init; } = Array.Empty<long>();

    [FromQuery(Name = "searchPattern")]
    public string SearchPattern { get; init; } = string.Empty;
}

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
    public async Task<IActionResult> GetPublicProfiles([FromMultiSource][BindRequired] GetPublicProfilesRequest request, CancellationToken ct)
    {
        // TODO: validate input

        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        IReadOnlyCollection<Contracts.User.ProfilePublic>? profiles = null;
        string source = string.Empty;

        if (request.UserIds.Length > 0 && string.IsNullOrWhiteSpace(request.SearchPattern))
        {
            profiles = await _profileClient.GetPublicProfiles(userClaims.UserId, request.UserIds, accessToken, ct);
            source = "in the ID list";
        }
        if (!string.IsNullOrWhiteSpace(request.SearchPattern) && request.UserIds.Length == 0)
        {
            profiles = await _profileClient.GetPublicProfiles(userClaims.UserId, request.SearchPattern, accessToken, ct);
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
}
