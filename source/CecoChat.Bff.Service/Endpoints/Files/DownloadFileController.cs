using System.Web;
using CecoChat.Server.Identity;
using CecoChat.User.Client;
using Common.AspNet.ModelBinding;
using Common.Minio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CecoChat.Bff.Service.Endpoints.Files;

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
    private readonly IFileClient _fileClient;

    public DownloadFileController(
        ILogger<DownloadFileController> logger,
        IMinioContext minio,
        IFileClient fileClient)
    {
        _logger = logger;
        _minio = minio;
        _fileClient = fileClient;
    }

    [Authorize(Policy = "user")]
    [HttpGet("{bucket}/{path}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task DownloadFile([FromMultiSource][BindRequired] DownloadFileRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        string bucket = request.BucketUrlDecoded;
        string path = request.PathUrlDecoded;

        bool hasAccess = await _fileClient.HasUserFileAccess(userClaims.UserId, bucket, path, accessToken, ct);
        if (!hasAccess)
        {
            _logger.LogWarning("File in bucket {Bucket} with path {Path} is being requested by user {UserId} without having access", bucket, path, userClaims.UserId);
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        ObjectMetadataResult objectMetadataResult = await _minio.GetObjectMetadata(bucket, path, ct);
        if (!objectMetadataResult.IsFound)
        {
            _logger.LogTrace("Failed to find the file in bucket {Bucket} with path {Path}", bucket, path);
            Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = objectMetadataResult.ContentType;
        Response.ContentLength = objectMetadataResult.Size;

        DownloadFileResult downloadFileResult = await _minio.WriteObjectToStream(bucket, path, Response.Body, ct);
        if (!downloadFileResult.IsFound)
        {
            _logger.LogTrace("Failed to find file in bucket {Bucket} with path {Path}", bucket, path);
            Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        _logger.LogTrace("Responding with file from bucket {Bucket} with path {Path} requested by user {UserId}", bucket, path, userClaims.UserId);
    }
}
