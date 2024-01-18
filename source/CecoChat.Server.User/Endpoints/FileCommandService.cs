using CecoChat.Contracts.User;
using CecoChat.Data.User.Entities.Files;
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
    public override async Task<AssociateFileResponse> AssociateFile(AssociateFileRequest request, ServerCallContext context)
    {
        UserClaims userClaims = context.GetUserClaimsGrpc(_logger);

        AssociateFileResult result = await _commandRepo.AssociateFile(userClaims.UserId, request.Bucket, request.Path);

        if (result.Success)
        {
            _logger.LogTrace("Responding with a successful association between a file in bucket {Bucket} with path {Path} and user {UserId}", request.Bucket, request.Path, userClaims.UserId);
            return new AssociateFileResponse
            {
                Success = true,
                Version = result.Version.ToTimestamp()
            };
        }
        if (result.Duplicate)
        {
            _logger.LogTrace("Responding with a duplicate association between the file in bucket {Bucket} with path {Path} and user {UserId}", request.Bucket, request.Path, userClaims.UserId);
            return new AssociateFileResponse
            {
                Duplicate = true
            };
        }

        throw new ProcessingFailureException(typeof(AssociateFileResult));
    }
}
