using CecoChat.Bff.Contracts;
using CecoChat.Bff.Contracts.Files;
using CecoChat.Bff.Service.Files;
using CecoChat.Data;
using CecoChat.Server.Identity;
using CecoChat.User.Client;
using Common;
using Common.AspNet.ModelBinding;
using Common.Seaweed;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Net.Http.Headers;
using ContentDispositionHeaderValue = System.Net.Http.Headers.ContentDispositionHeaderValue;

namespace CecoChat.Bff.Service.Endpoints.Files;

public sealed class UploadFileRequest
{
    [FromHeader(Name = "Content-Length")]
    public long FileSize { get; init; }

    [FromHeader(Name = IBffClient.HeaderUploadedFileAllowedUserId)]
    public long AllowedUserId { get; init; }
}

[ApiController]
[Route("api/files")]
[ApiExplorerSettings(GroupName = "Files")]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class UploadFileController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly ISeaweedContext _seaweed;
    private readonly IFileUtility _fileUtility;
    private readonly IObjectNaming _objectNaming;
    private readonly IFileClient _fileClient;

    public UploadFileController(
        ILogger<UploadFileController> logger,
        ISeaweedContext seaweed,
        IFileUtility fileUtility,
        IObjectNaming objectNaming,
        IFileClient fileClient)
    {
        _logger = logger;
        _seaweed = seaweed;
        _fileUtility = fileUtility;
        _objectNaming = objectNaming;
        _fileClient = fileClient;
    }

    [Authorize(Policy = "user")]
    [HttpPost]
    [ProducesResponseType(typeof(UploadFileResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadFile([FromMultiSource][BindRequired] UploadFileRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        PrepareUploadResult prepareUploadResult = PrepareUpload();
        if (prepareUploadResult.Failure != null)
        {
            return prepareUploadResult.Failure;
        }

        UploadFileResult uploadFileResult = await UploadFile(userClaims, prepareUploadResult.FileExtension, prepareUploadResult.FileContentType, Request.Body, request.FileSize, ct);

        AssociateFileResult associateFileResult = await AssociateFile(userClaims, uploadFileResult.Bucket, uploadFileResult.Path, request.AllowedUserId, accessToken, ct);
        if (associateFileResult.Failure != null)
        {
            return associateFileResult.Failure;
        }

        UploadFileResponse response = new();
        response.File = new FileRef
        {
            Bucket = uploadFileResult.Bucket,
            Path = uploadFileResult.Path,
            Version = associateFileResult.FileVersion
        };

        return Ok(response);
    }

    private struct PrepareUploadResult
    {
        public string FileExtension { get; init; }
        public string FileContentType { get; init; }
        public IActionResult? Failure { get; init; }
    }

    private PrepareUploadResult PrepareUpload()
    {
        if (!ContentDispositionHeaderValue.TryParse(Request.Headers.ContentDisposition.ToString(), out ContentDispositionHeaderValue? contentDisposition))
        {
            ModelState.AddModelError("File", "The Content-Disposition header with the file name is missing or invalid.");
            return new PrepareUploadResult
            {
                Failure = BadRequest(ModelState)
            };
        }

        string? headerValue = contentDisposition.FileNameStar ?? contentDisposition.FileName;
        string fileName = HeaderUtilities.RemoveQuotes(headerValue).Value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(fileName))
        {
            ModelState.AddModelError("File", "The Content-Disposition header with the file name is empty.");
            return new PrepareUploadResult
            {
                Failure = BadRequest(ModelState)
            };
        }

        string extension = Path.GetExtension(fileName);
        if (!_fileUtility.IsExtensionKnown(extension))
        {
            ModelState.AddModelError("File", $"File extension '{extension}' is not supported.");
            return new PrepareUploadResult
            {
                Failure = BadRequest(ModelState)
            };
        }

        string contentType = _fileUtility.GetContentType(extension);
        if (!string.Equals(Request.ContentType, contentType, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("File", $"Provided content type '{Request.ContentType}' doesn't match the content type of the file.");
            return new PrepareUploadResult
            {
                Failure = BadRequest(ModelState)
            };
        }

        return new PrepareUploadResult
        {
            FileExtension = extension,
            FileContentType = contentType
        };
    }

    private struct UploadFileResult
    {
        public string Bucket { get; init; }
        public string Path { get; init; }
    }

    private async Task<UploadFileResult> UploadFile(UserClaims userClaims, string fileExtension, string fileContentType, Stream fileStream, long fileSize, CancellationToken ct)
    {
        string bucketName = _objectNaming.GetCurrentBucketName();
        string objectName = _objectNaming.CreateObjectName(userClaims.UserId, fileExtension);

        await _seaweed.UploadObject(bucketName, objectName, fileContentType, fileStream, fileSize, ct);
        _logger.LogTrace("Uploaded successfully a new file with content type {ContentType} sized {FileSize}B to bucket {Bucket} with path {Path} for user {UserId}",
            fileContentType, fileSize, bucketName, objectName, userClaims.UserId);

        return new UploadFileResult
        {
            Bucket = bucketName,
            Path = objectName
        };
    }

    private struct AssociateFileResult
    {
        public DateTime FileVersion { get; init; }
        public IActionResult? Failure { get; init; }
    }

    private async Task<AssociateFileResult> AssociateFile(UserClaims userClaims, string bucket, string path, long allowedUserId, string accessToken, CancellationToken ct)
    {
        User.Client.AssociateFileResult result = await _fileClient.AssociateFile(userClaims.UserId, bucket, path, allowedUserId, accessToken, ct);

        if (result.Success)
        {
            _logger.LogTrace("Associated successfully a new file in bucket {Bucket} with path {Path} and user {UserId}", bucket, path, userClaims.UserId);
            return new AssociateFileResult
            {
                FileVersion = result.Version
            };
        }
        if (result.Duplicate)
        {
            _logger.LogTrace("Association already exists between a file in bucket {Bucket} with path {Path} and user {UserId}", bucket, path, userClaims.UserId);
            IActionResult failure = Conflict(new ProblemDetails
            {
                Detail = "Duplicate file"
            });

            return new AssociateFileResult
            {
                Failure = failure
            };
        }

        throw new ProcessingFailureException(typeof(User.Client.AssociateFileResult));
    }
}
