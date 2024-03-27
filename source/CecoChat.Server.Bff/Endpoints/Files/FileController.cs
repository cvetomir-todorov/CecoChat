using AutoMapper;
using CecoChat.Client.User;
using CecoChat.Contracts.Bff.Files;
using CecoChat.Server.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CecoChat.Server.Bff.Endpoints.Files;

[ApiController]
[Route("api/files")]
[ApiExplorerSettings(GroupName = "Files")]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class FileController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IFileClient _fileClient;

    public FileController(
        ILogger<FileController> logger,
        IMapper mapper,
        IFileClient fileClient)
    {
        _logger = logger;
        _mapper = mapper;
        _fileClient = fileClient;
    }

    [Authorize(Policy = "user")]
    [HttpGet("list")]
    [ProducesResponseType(typeof(GetUserFilesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserFiles([FromQuery][BindRequired] GetUserFilesRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        IReadOnlyCollection<User.Contracts.FileRef> serviceFiles = await _fileClient.GetUserFiles(userClaims.UserId, request.NewerThan, accessToken, ct);
        FileRef[] files = _mapper.Map<FileRef[]>(serviceFiles)!;

        GetUserFilesResponse response = new()
        {
            Files = files
        };

        _logger.LogTrace("Responding with {FileCount} files for user {UserId} which are newer than {NewerThan}", files.Length, userClaims.UserId, request.NewerThan);
        return Ok(response);
    }
}
