using System.Web;
using CecoChat.Contracts.Bff.Files;
using CecoChat.Server.Identity;
using CecoChat.User.Client;
using Common;
using Common.AspNet.ModelBinding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CecoChat.Server.Bff.Endpoints.Files;

public sealed class AddFileAccessRoute
{
    [FromRoute(Name = "bucket")]
    public string Bucket { get; init; } = string.Empty;

    public string BucketUrlDecoded => HttpUtility.UrlDecode(Bucket);

    [FromRoute(Name = "path")]
    public string Path { get; init; } = string.Empty;

    public string PathUrlDecoded => HttpUtility.UrlDecode(Path);
}

[ApiController]
[Route("api/files")]
[ApiExplorerSettings(GroupName = "Files")]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class FileAccessController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IFileClient _fileClient;

    public FileAccessController(
        ILogger<FileAccessController> logger,
        IFileClient fileClient)
    {
        _logger = logger;
        _fileClient = fileClient;
    }

    [Authorize(Policy = "user")]
    [HttpPut("{bucket}/{path}/access")]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(AddFileAccessResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddFileAccess(
        [FromMultiSource][BindRequired] AddFileAccessRoute route,
        [FromBody][BindRequired] AddFileAccessRequest request,
        CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        string bucket = route.BucketUrlDecoded;
        string path = route.PathUrlDecoded;

        AddFileAccessResult result = await _fileClient.AddFileAccess(userClaims.UserId, bucket, path, request.Version, request.AllowedUserId, accessToken, ct);

        if (result.Success)
        {
            _logger.LogTrace(
                "Added file-access for user ID {AllowedUserId} to file in bucket {Bucket} with path {Path} owned by user {UserId}",
                request.AllowedUserId, bucket, path, userClaims.UserId);
            return Ok(new AddFileAccessResponse
            {
                NewVersion = result.NewVersion
            });
        }
        if (result.ConcurrentlyUpdated)
        {
            _logger.LogTrace(
                "Failed to add file-access for user ID {AllowedUserId} to file in bucket {Bucket} with path {Path} owned by user {UserId} because the file has been concurrently updated",
                request.AllowedUserId, bucket, path, userClaims.UserId);
            return Conflict(new ProblemDetails
            {
                Detail = "Concurrently updated"
            });
        }

        throw new ProcessingFailureException(typeof(AddFileAccessResult));
    }
}
