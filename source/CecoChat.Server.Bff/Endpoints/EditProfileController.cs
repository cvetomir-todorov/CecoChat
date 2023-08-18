using AutoMapper;
using CecoChat.Client.User;
using CecoChat.Contracts.Bff;
using CecoChat.Server.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CecoChat.Server.Bff.Endpoints;

[ApiController]
[Route("api/user")]
public class EditProfileController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IUserClient _userClient;

    public EditProfileController(ILogger<EditProfileController> logger, IMapper mapper, IUserClient userClient)
    {
        _logger = logger;
        _mapper = mapper;
        _userClient = userClient;
    }

    [Authorize(Policy = "user")]
    [HttpPost("password", Name = "ChangePassword")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ChangePassword([FromBody][BindRequired] ChangePasswordRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        Contracts.User.ProfileChangePassword profile = _mapper.Map<Contracts.User.ProfileChangePassword>(request);

        ChangePasswordResult result = await _userClient.ChangePassword(profile, userClaims.UserId, accessToken, ct);
        if (result.Success)
        {
            ChangePasswordResponse response = new() { NewVersion = result.NewVersion };
            _logger.LogTrace("Responding with successful password change for user {UserId}", userClaims.UserId);
            return Ok(response);
        }
        if (result.ConcurrentlyUpdated)
        {
            _logger.LogTrace("Responding with failed password change for user {UserId} since the profile has been concurrently updated", userClaims.UserId);
            return Conflict();
        }

        throw new InvalidOperationException($"Failed to process {nameof(ChangePasswordResult)}.");
    }

    [Authorize(Policy = "user")]
    [HttpPost(Name = "EditProfile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> EditProfile([FromBody][BindRequired] EditProfileRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        Contracts.User.ProfileUpdate profile = _mapper.Map<Contracts.User.ProfileUpdate>(request);

        UpdateProfileResult result = await _userClient.UpdateProfile(profile, userClaims.UserId, accessToken, ct);
        if (result.Success)
        {
            EditProfileResponse response = new() { NewVersion = result.NewVersion };
            _logger.LogTrace("Responding with successful profile edit for user {UserId}", userClaims.UserId);
            return Ok(response);
        }
        if (result.ConcurrentlyUpdated)
        {
            _logger.LogTrace("Responding with failed profile edit for user {UserId} since it has been concurrently edited", userClaims.UserId);
            return Conflict();
        }

        throw new InvalidOperationException($"Failed to process {nameof(UpdateProfileResult)}.");
    }
}
