using CecoChat.Contracts.User;
using CecoChat.Data.User.Files;
using CecoChat.Server.Identity;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace CecoChat.Server.User.Endpoints;

public class FileCommandService : FileCommand.FileCommandBase
{
    private readonly ILogger _logger;
    private readonly IFileCommandRepo _commandRepo;

    public FileCommandService(
        ILogger<FileCommandService> logger,
        IFileCommandRepo commandRepo)
    {
        _logger = logger;
        _commandRepo = commandRepo;
    }

    [Authorize(Policy = "user")]
    public override async Task<AddFileResponse> AddFile(AddFileRequest request, ServerCallContext context)
    {
        UserClaims userClaims = context.GetUserClaimsGrpc(_logger);

        AddFileResult result = await _commandRepo.AddFile(userClaims.UserId, request.Bucket, request.Path);

        if (result.Success)
        {
            _logger.LogTrace("Responding with successful addition of file to bucket {Bucket} with path {Path} for user {UserId}", request.Bucket, request.Path, userClaims.UserId);
            return new AddFileResponse
            {
                Success = true,
                Version = result.Version.ToTimestamp()
            };
        }
        if (result.DuplicateFile)
        {
            _logger.LogTrace("Responding with failed addition of duplicate file to bucket {Bucket} with path {Path} for user {UserId}", request.Bucket, request.Path, userClaims.UserId);
            return new AddFileResponse
            {
                DuplicateFile = true
            };
        }

        throw new ProcessingFailureException(typeof(AddFileResult));
    }
}
