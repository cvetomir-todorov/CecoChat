using System.Web;
using CecoChat.AspNet.ModelBinding;
using CecoChat.Minio;
using CecoChat.Server.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CecoChat.Server.Bff.Endpoints.Files;

public sealed class DownloadFileRequest
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
public class DownloadFileController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IMinioContext _minio;

    public DownloadFileController(
        ILogger<DownloadFileController> logger,
        IMinioContext minio)
    {
        _logger = logger;
        _minio = minio;
    }

    [Authorize(Policy = "user")]
    [HttpGet("{bucket}/{path}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadFile([FromMultiSource][BindRequired] DownloadFileRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out _))
        {
            return Unauthorized();
        }

        string bucket = request.BucketUrlDecoded;
        string path = request.PathUrlDecoded;

        ObjectTagsResult objectTagsResult = await _minio.GetObjectTags(bucket, path, ct);
        if (!objectTagsResult.IsFound)
        {
            _logger.LogTrace("Failed to find file in bucket {Bucket} with path {Path}", bucket, path);
            return NotFound();
        }
        if (!objectTagsResult.Tags.TryGetValue("users", out string? usersTag))
        {
            _logger.LogWarning("File in bucket {Bucket} with path {Path} is missing the users tag", bucket, path);
            return Forbid();
        }

        bool containsCurrentUserId = ContainsCurrentUserId(usersTag, userClaims.UserId);
        if (!containsCurrentUserId)
        {
            _logger.LogWarning("File in bucket {Bucket} with path {Path} is being requested by user {UserId} but doesn't have the expected users tag", bucket, path, userClaims.UserId);
            return Forbid();
        }

        DownloadFileResult downloadFileResult = await _minio.DownloadFile(bucket, path, ct);
        if (!downloadFileResult.IsFound)
        {
            _logger.LogTrace("Failed to find file in bucket {Bucket} with path {Path}", bucket, path);
            return NotFound();
        }

        _logger.LogTrace("Responding with file from bucket {Bucket} with path {Path} requested by user {UserId}", bucket, path, userClaims.UserId);
        return File(downloadFileResult.Stream, downloadFileResult.ContentType);
    }

    private static bool ContainsCurrentUserId(string usersTag, long currentUserId)
    {
        string[] userIdStrings = usersTag.Split(separator: '_', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        bool containsCurrentUserId = userIdStrings
            .Select(userIdString =>
            {
                if (!long.TryParse(userIdString, out long userId))
                {
                    userId = 0;
                }

                return userId;
            })
            .Where(userId => userId > 0)
            .Any(userId => userId == currentUserId);

        return containsCurrentUserId;
    }
}
