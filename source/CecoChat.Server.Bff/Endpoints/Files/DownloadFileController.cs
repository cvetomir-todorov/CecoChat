using System.Web;
using CecoChat.Minio;
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
    public async Task<IActionResult> DownloadFile(
        [FromRoute(Name = "bucket")][BindRequired] string urlEncodedBucket,
        [FromRoute(Name = "path")][BindRequired] string urlEncodedPath,
        CancellationToken ct)
    {
        string bucket = HttpUtility.UrlDecode(urlEncodedBucket);
        string path = HttpUtility.UrlDecode(urlEncodedPath);

        DownloadFileResult result = await _minio.DownloadFile(bucket, path, ct);
        if (!result.IsFound)
        {
            _logger.LogTrace("Failed to find file in bucket {Bucket} with path {Path}", bucket, path);
            return NotFound();
        }

        // TODO: verify the current user has access to the file

        return File(result.Stream, result.ContentType);
    }
}
