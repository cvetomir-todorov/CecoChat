using CecoChat.Contracts.User;
using CecoChat.Data.User.Files;
using CecoChat.Server.Identity;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace CecoChat.Server.User.Endpoints;

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
}
