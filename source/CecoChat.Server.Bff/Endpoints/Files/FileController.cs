using System.Globalization;
using CecoChat.AspNet;
using CecoChat.AspNet.ModelBinding;
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

    public FileController(
        ILogger<FileController> logger,
        IMinioContext minio,
        IFileStorage fileStorage)
    {
        _logger = logger;
        _minio = minio;
        _fileStorage = fileStorage;
    }

    [Authorize(Policy = "user")]
    [HttpPost]
    [RequestSizeLimit(FileSizeLimitBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = FileSizeLimitBytes)]
    [DisableFormValueModelBinding]
    [ProducesResponseType(typeof(UploadFileResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadFile([FromHeader(Name = IBffClient.HeaderUploadedFileSize)] long uploadedFileSize, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out _))
        {
            return Unauthorized();
        }
        if (Request.ContentLength == null || Request.ContentLength.Value == 0)
        {
            ModelState.AddModelError("Request", "The request content-length should be set to match the uploaded file size.");
            return BadRequest(ModelState);
        }
        if (!MultipartUtility.IsMultipartContentType(Request.ContentType))
        {
            ModelState.AddModelError("File", "The request content-type should be multipart.");
            return BadRequest(ModelState);
        }

        string boundary = MultipartUtility.GetMultipartBoundary(Request.ContentType);
        MultipartReader reader = new(boundary, Request.Body);
        MultipartSection? section = await reader.ReadNextSectionAsync(ct);
        FileMultipartSection? fileSection = section?.AsFileSection();
        if (fileSection == null || fileSection.FileStream == null)
        {
            ModelState.AddModelError("File", "There is no file multipart section.");
            return BadRequest(ModelState);
        }

        string bucketName = _fileStorage.GetCurrentBucketName();
        string extensionWithDot = Path.GetExtension(fileSection.FileName);
        string plannedObjectName = _fileStorage.CreateObjectName(userClaims.UserId, extensionWithDot);
        IDictionary<string, string> tags = new SortedList<string, string>(capacity: 1);
        tags.Add("user-id", userClaims.UserId.ToString(CultureInfo.InvariantCulture));

        string actualObjectName = await _minio.UploadFile(bucketName, plannedObjectName, tags, fileSection.FileStream, uploadedFileSize, ct);

        return Ok(new UploadFileResponse
        {
            FilePath = actualObjectName
        });
    }
}
