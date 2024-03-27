using CecoChat.Server.Identity;
using CecoChat.User.Contracts;
using CecoChat.User.Data.Entities.Files;
using Common;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace CecoChat.User.Service.Endpoints.Files;

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

        AssociateFileResult result = await _commandRepo.AssociateFile(userClaims.UserId, request.Bucket, request.Path, request.AllowedUserId);

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

    [Authorize(Policy = "user")]
    public override async Task<AddFileAccessResponse> AddFileAccess(AddFileAccessRequest request, ServerCallContext context)
    {
        UserClaims userClaims = context.GetUserClaimsGrpc(_logger);

        AddFileAccessResult result = await _commandRepo.AddFileAccess(userClaims.UserId, request.Bucket, request.Path, request.Version.ToDateTime(), request.AllowedUserId);

        if (result.Success)
        {
            _logger.LogTrace(
                "Responding with a successful file-access addition for user ID {AllowedUserId} to file in bucket {Bucket} with path {Path} owned by user {UserId}",
                request.AllowedUserId, request.Bucket, request.Path, userClaims.UserId);
            return new AddFileAccessResponse
            {
                Success = true,
                NewVersion = result.NewVersion.ToTimestamp()
            };
        }
        if (result.ConcurrentlyUpdated)
        {
            _logger.LogTrace(
                "Responding with a failed file-access addition for user ID {AllowedUserId} to file in bucket {Bucket} with path {Path} owned by user {UserId} because the file has been concurrently updated",
                request.AllowedUserId, request.Bucket, request.Path, userClaims.UserId);
            return new AddFileAccessResponse
            {
                ConcurrentlyUpdated = true
            };
        }

        throw new ProcessingFailureException(typeof(AddFileAccessResult));
    }
}
