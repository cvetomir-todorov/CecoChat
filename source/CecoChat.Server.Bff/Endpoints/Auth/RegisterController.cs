using AutoMapper;
using CecoChat.Contracts.Bff.Auth;
using CecoChat.User.Client;
using Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CecoChat.Server.Bff.Endpoints.Auth;

[ApiController]
[Route("api")]
[ApiExplorerSettings(GroupName = "Auth")]
public class RegisterController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IAuthClient _authClient;

    public RegisterController(
        ILogger<RegisterController> logger,
        IMapper mapper,
        IAuthClient authClient)
    {
        _logger = logger;
        _mapper = mapper;
        _authClient = authClient;
    }

    [AllowAnonymous]
    [HttpPost("registration", Name = "Registration")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register([FromBody][BindRequired] RegisterRequest request, CancellationToken ct)
    {
        User.Contracts.Registration registration = _mapper.Map<User.Contracts.Registration>(request)!;
        registration.AvatarUrl = $"https://cdn.cecochat.com/avatars/{request.UserName}.jpg";

        RegisterResult result = await _authClient.Register(registration, ct);

        if (result.Success)
        {
            _logger.LogTrace("Responding with confirmation for successfully creating a profile for user {UserName}", request.UserName);

            request.Password = null!;
            return CreatedAtAction(nameof(Register), request);
        }
        if (result.DuplicateUserName)
        {
            _logger.LogTrace("Responding with failure to create a profile for user {UserName} because of a duplicate user name", request.UserName);
            return Conflict(new ProblemDetails
            {
                Detail = "Duplicate user name"
            });
        }

        throw new ProcessingFailureException(typeof(RegisterResult));
    }
}
