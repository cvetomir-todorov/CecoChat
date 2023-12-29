using System.Globalization;
using CecoChat.AspNet;
using CecoChat.AspNet.ModelBinding;
using CecoChat.Client.User;
using CecoChat.Contracts.Bff;
using CecoChat.Contracts.Bff.Files;
using CecoChat.Minio;
using CecoChat.Server.Bff.Files;
using CecoChat.Server.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

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
    private const int FileSizeLimitBytes = 512 * 1024; // 512KB
    private readonly ILogger _logger;
    private readonly IMinioContext _minio;
    private readonly IFileStorage _fileStorage;
    private readonly IFileClient _fileClient;

    public FileController(
        ILogger<FileController> logger,
        IMinioContext minio,
        IFileStorage fileStorage,
        IFileClient fileClient)
    {
        _logger = logger;
        _minio = minio;
        _fileStorage = fileStorage;
        _fileClient = fileClient;
    }

    [Authorize(Policy = "user")]
    [HttpPost]
    [RequestSizeLimit(FileSizeLimitBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = FileSizeLimitBytes)]
    [DisableFormValueModelBinding]
    [ProducesResponseType(typeof(UploadFileResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadFile([FromHeader(Name = IBffClient.HeaderUploadedFileSize)] long fileSize, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        UploadFileResult uploadFileResult = await UploadFile(userClaims, Request.ContentType, Request.Body, fileSize, ct);
        if (uploadFileResult.Failure != null)
        {
            return uploadFileResult.Failure;
        }

        (DateTime fileVersion, IActionResult? failure) = await TryAddFileForUser(userClaims, uploadFileResult.Bucket, uploadFileResult.Path, accessToken, ct);
        if (failure != null)
        {
            return failure;
        }

        return Ok(new UploadFileResponse
        {
            Bucket = uploadFileResult.Bucket,
            Path = uploadFileResult.Path,
            Version = fileVersion
        });
    }

    private struct UploadFileResult
    {
        public string Bucket { get; set; }
        public string Path { get; set; }
        public IActionResult? Failure { get; set; }
    }

    private async Task<UploadFileResult> UploadFile(UserClaims userClaims, string? contentType, Stream body, long fileSize, CancellationToken ct)
    {
        if (!MultipartUtility.IsMultipartContentType(contentType))
        {
            ModelState.AddModelError("File", "The request content-type should be multipart.");
            return new UploadFileResult
            {
                Failure = BadRequest(ModelState)
            };
        }

        string boundary = MultipartUtility.GetMultipartBoundary(contentType);
        MultipartReader reader = new(boundary, body);
        MultipartSection? section = await reader.ReadNextSectionAsync(ct);
        FileMultipartSection? fileSection = section?.AsFileSection();
        if (fileSection == null || fileSection.FileStream == null)
        {
            ModelState.AddModelError("File", "There is no file multipart section.");
            return new UploadFileResult
            {
                Failure = BadRequest(ModelState)
            };
        }

        string bucketName = _fileStorage.GetCurrentBucketName();
        string extensionWithDot = Path.GetExtension(fileSection.FileName);
        string plannedObjectName = _fileStorage.CreateObjectName(userClaims.UserId, extensionWithDot);
        IDictionary<string, string> tags = new SortedList<string, string>(capacity: 1);
        tags.Add("user-id", userClaims.UserId.ToString(CultureInfo.InvariantCulture));

        string actualObjectName = await _minio.UploadFile(bucketName, plannedObjectName, tags, fileSection.FileStream, fileSize, ct);
        _logger.LogTrace("Uploaded successfully a new file sized {FileSize} bytes to bucket {Bucket} with path {Path} for user {UserId}", fileSize, bucketName, actualObjectName, userClaims.UserId);

        return new UploadFileResult
        {
            Bucket = bucketName,
            Path = actualObjectName
        };
    }

    private async Task<(DateTime, IActionResult?)> TryAddFileForUser(UserClaims userClaims, string bucket, string path, string accessToken, CancellationToken ct)
    {
        AddFileResult result = await _fileClient.AddFile(userClaims.UserId, bucket, path, accessToken, ct);

        if (result.Success)
        {
            _logger.LogTrace("Associated successfully a new file in bucket {Bucket} with path {Path} and user {UserId}", bucket, path, userClaims.UserId);
            return (result.Version, null);
        }
        if (result.DuplicateFile)
        {
            _logger.LogTrace("Association failed for a duplicate file in bucket {Bucket} with path {Path} and user {UserId}", bucket, path, userClaims.UserId);
            return (DateTime.MinValue, Conflict(new ProblemDetails
            {
                Detail = "Duplicate file"
            }));
        }

        throw new ProcessingFailureException(typeof(AddFileResult));
    }
}
