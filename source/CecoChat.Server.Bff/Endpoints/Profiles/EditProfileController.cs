using AutoMapper;
using CecoChat.Contracts.Bff.Profiles;
using CecoChat.Server.Identity;
using CecoChat.User.Client;
using Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CecoChat.Server.Bff.Endpoints.Profiles;

[ApiController]
[Route("api/profile")]
[ApiExplorerSettings(GroupName = "Profiles")]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class EditProfileController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IProfileClient _profileClient;

    public EditProfileController(
        ILogger<EditProfileController> logger,
        IMapper mapper,
        IProfileClient profileClient)
    {
        _logger = logger;
        _mapper = mapper;
        _profileClient = profileClient;
    }

    [Authorize(Policy = "user")]
    [HttpPut("password", Name = "ChangePassword")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangePassword([FromBody][BindRequired] ChangePasswordRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        ChangePasswordResult result = await _profileClient.ChangePassword(request.NewPassword, request.Version, userClaims.UserId, accessToken, ct);
        if (result.Success)
        {
            ChangePasswordResponse response = new() { NewVersion = result.NewVersion };
            _logger.LogTrace("Responding with successful password change for user {UserId}", userClaims.UserId);
            return Ok(response);
        }
        if (result.ConcurrentlyUpdated)
        {
            _logger.LogTrace("Responding with failed password change for user {UserId} since the profile has been concurrently updated", userClaims.UserId);
            return Conflict(new ProblemDetails
            {
                Detail = "Concurrently updated"
            });
        }

        throw new ProcessingFailureException(typeof(ChangePasswordResult));
    }

    [Authorize(Policy = "user")]
    [HttpPut(Name = "EditProfile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EditProfile([FromBody][BindRequired] EditProfileRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        User.Contracts.ProfileUpdate profile = _mapper.Map<User.Contracts.ProfileUpdate>(request)!;

        UpdateProfileResult result = await _profileClient.UpdateProfile(profile, userClaims.UserId, accessToken, ct);
        if (result.Success)
        {
            EditProfileResponse response = new() { NewVersion = result.NewVersion };
            _logger.LogTrace("Responding with successful profile edit for user {UserId}", userClaims.UserId);
            return Ok(response);
        }
        if (result.ConcurrentlyUpdated)
        {
            _logger.LogTrace("Responding with failed profile edit for user {UserId} since it has been concurrently edited", userClaims.UserId);
            return Conflict(new ProblemDetails
            {
                Detail = "Concurrently updated"
            });
        }

        throw new ProcessingFailureException(typeof(UpdateProfileResult));
    }
}
