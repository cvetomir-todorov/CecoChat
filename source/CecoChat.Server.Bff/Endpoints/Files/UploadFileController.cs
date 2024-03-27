using CecoChat.Contracts.Bff;
using CecoChat.Contracts.Bff.Files;
using CecoChat.Data;
using CecoChat.Server.Bff.Files;
using CecoChat.Server.Identity;
using CecoChat.User.Client;
using Common;
using Common.AspNet;
using Common.AspNet.ModelBinding;
using Common.Minio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;

namespace CecoChat.Server.Bff.Endpoints.Files;

public sealed class UploadFileRequest
{
    [FromHeader(Name = IBffClient.HeaderUploadedFileSize)]
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
    private readonly IMinioContext _minio;
    private readonly IFileUtility _fileUtility;
    private readonly IObjectNaming _objectNaming;
    private readonly IFileClient _fileClient;

    public UploadFileController(
        ILogger<UploadFileController> logger,
        IMinioContext minio,
        IFileUtility fileUtility,
        IObjectNaming objectNaming,
        IFileClient fileClient)
    {
        _logger = logger;
        _minio = minio;
        _fileUtility = fileUtility;
        _objectNaming = objectNaming;
        _fileClient = fileClient;
    }

    [Authorize(Policy = "user")]
    [HttpPost]
    [DisableFormValueModelBinding]
    [ProducesResponseType(typeof(UploadFileResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadFile([FromMultiSource][BindRequired] UploadFileRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        PrepareUploadResult prepareUploadResult = await PrepareUpload(Request.ContentType ?? string.Empty, Request.Body, ct);
        if (prepareUploadResult.Failure != null)
        {
            return prepareUploadResult.Failure;
        }

        UploadFileResult uploadFileResult = await UploadFile(userClaims, prepareUploadResult.FileExtension, prepareUploadResult.FileContentType, prepareUploadResult.FileStream, request.FileSize, ct);

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
        public Stream FileStream { get; init; }
        public IActionResult? Failure { get; init; }
    }

    private async Task<PrepareUploadResult> PrepareUpload(string requestContentType, Stream requestBody, CancellationToken ct)
    {
        if (!MultipartUtility.IsMultipartContentType(requestContentType))
        {
            ModelState.AddModelError("File", "The request content type should be multipart.");
            return new PrepareUploadResult
            {
                Failure = BadRequest(ModelState)
            };
        }

        string boundary = MultipartUtility.GetMultipartBoundary(requestContentType);
        MultipartReader reader = new(boundary, requestBody);
        MultipartSection? section = await reader.ReadNextSectionAsync(ct);
        FileMultipartSection? fileSection = section?.AsFileSection();
        if (section == null || fileSection == null || fileSection.FileStream == null)
        {
            ModelState.AddModelError("File", "There is no file multipart section.");
            return new PrepareUploadResult
            {
                Failure = BadRequest(ModelState)
            };
        }

        string extension = Path.GetExtension(fileSection.FileName);
        if (!_fileUtility.IsExtensionKnown(extension))
        {
            ModelState.AddModelError("File", $"File extension '{extension}' is not supported.");
            return new PrepareUploadResult
            {
                Failure = BadRequest(ModelState)
            };
        }

        if (string.IsNullOrWhiteSpace(section.ContentType))
        {
            ModelState.AddModelError("File", "File content type is not specified.");
            return new PrepareUploadResult
            {
                Failure = BadRequest(ModelState)
            };
        }

        string correspondingContentType = _fileUtility.GetContentType(extension);
        if (!string.Equals(section.ContentType, correspondingContentType, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("File", $"Provided content type '{section.ContentType}' doesn't have the correct value.");
            return new PrepareUploadResult
            {
                Failure = BadRequest(ModelState)
            };
        }

        return new PrepareUploadResult
        {
            FileExtension = extension,
            FileContentType = correspondingContentType,
            FileStream = fileSection.FileStream
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
        string plannedObjectName = _objectNaming.CreateObjectName(userClaims.UserId, fileExtension);

        string actualObjectName = await _minio.UploadFile(bucketName, plannedObjectName, fileContentType, tags: null, fileStream, fileSize, ct);
        _logger.LogTrace("Uploaded successfully a new file with content type {ContentType} sized {FileSize}B to bucket {Bucket} with path {Path} for user {UserId}",
            fileContentType, fileSize, bucketName, actualObjectName, userClaims.UserId);

        return new UploadFileResult
        {
            Bucket = bucketName,
            Path = actualObjectName
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
