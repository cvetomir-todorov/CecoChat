using CecoChat.Data.User.Entities.Files;
using CecoChat.Server.Identity;
using CecoChat.User.Contracts;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace CecoChat.Server.User.Endpoints.Files;

public class FileQueryService : FileQuery.FileQueryBase
{
    private readonly ILogger _logger;
    private readonly IFileQueryRepo _queryRepo;

    public FileQueryService(
        ILogger<FileQueryService> logger,
        IFileQueryRepo queryRepo)
    {
        _logger = logger;
        _queryRepo = queryRepo;
    }

    [Authorize(Policy = "user")]
    public override async Task<GetUserFilesResponse> GetUserFiles(GetUserFilesRequest request, ServerCallContext context)
    {
        UserClaims userClaims = context.GetUserClaimsGrpc(_logger);

        IEnumerable<FileRef> files = await _queryRepo.GetUserFiles(userClaims.UserId, request.NewerThan.ToDateTime());

        GetUserFilesResponse response = new();
        response.Files.AddRange(files);

        _logger.LogTrace("Responding with {FileCount} files for user {UserId} which are newer than {NewerThan}", response.Files.Count, userClaims.UserId, request.NewerThan);
        return response;
    }

    [Authorize(Policy = "user")]
    public override async Task<HasUserFileAccessResponse> HasUserFileAccess(HasUserFileAccessRequest request, ServerCallContext context)
    {
        UserClaims userClaims = context.GetUserClaimsGrpc(_logger);

        bool hasAccess = await _queryRepo.HasUserFileAccess(userClaims.UserId, request.Bucket, request.Path);

        HasUserFileAccessResponse response = new();
        response.HasAccess = hasAccess;

        _logger.LogTrace(
            "Responding with user {UserId} {Access} to file in bucket {Bucket} with path {Path}",
            userClaims.UserId, response.HasAccess ? "has access" : "has no access", request.Bucket, request.Path);
        return response;
    }
}
