using AutoMapper;
using CecoChat.Client.User;
using CecoChat.Contracts.Bff;
using CecoChat.Contracts.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CecoChat.Server.Bff.Endpoints;

[ApiController]
[Route("api")]
public class RegisterController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IUserClient _userClient;

    public RegisterController(
        ILogger<RegisterController> logger,
        IMapper mapper,
        IUserClient userClient)
    {
        _logger = logger;
        _mapper = mapper;
        _userClient = userClient;
    }

    [AllowAnonymous]
    [HttpPost("registration", Name = "Registration")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register([FromBody][BindRequired] RegisterRequest request, CancellationToken ct)
    {
        ProfileCreate profile = _mapper.Map<ProfileCreate>(request);
        profile.AvatarUrl = $"https://cdn.cecochat.com/avatars/{request.UserName}.jpg";

        CreateProfileResult result = await _userClient.CreateProfile(profile, ct);

        if (result.Success)
        {
            _logger.LogTrace("Responding with confirmation for successfully creating a profile for user {UserName}", request.UserName);

            request.Password = null!;
            return CreatedAtAction(nameof(Register), request);
        }
        if (result.DuplicateUserName)
        {
            _logger.LogTrace("Responding with failure to create a profile for user {UserName} because of a duplicate user name", request.UserName);
            return Conflict(new ProblemDetails { Title = "Duplicate user name" });
        }

        throw new InvalidOperationException($"Failed to process {nameof(CreateProfileResult)}.");
    }
}
