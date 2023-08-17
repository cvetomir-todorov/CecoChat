using CecoChat.Contracts;
using CecoChat.Contracts.User;
using CecoChat.Data.User.Contacts;
using CecoChat.Server.Identity;
using Grpc.Core;

namespace CecoChat.Server.User.Endpoints;

public class ContactCommandService : ContactCommand.ContactCommandBase
{
    private readonly ILogger _logger;
    private readonly IContactQueryRepo _queryRepo;
    private readonly IContactCommandRepo _commandRepo;

    public ContactCommandService(
        ILogger<ContactCommandService> logger,
        IContactQueryRepo queryRepo,
        IContactCommandRepo commandRepo)
    {
        _logger = logger;
        _queryRepo = queryRepo;
        _commandRepo = commandRepo;
    }

    public override async Task<InviteResponse> Invite(InviteRequest request, ServerCallContext context)
    {
        UserClaims userClaims = GetUserClaims(context);

        Contact? existingContact = await _queryRepo.GetContact(userClaims.UserId, request.ContactUserId);
        if (existingContact != null)
        {
            _logger.LogTrace("Responding with failed new request from user {UserId} to contact {ContactUserId} because the connection has already exists", userClaims.UserId, request.ContactUserId);
            return new InviteResponse
            {
                AlreadyExists = true
            };
        }

        Contact contact = new()
        {
            ContactUserId = request.ContactUserId,
            Status = ContactStatus.Pending,
            TargetUserId = request.ContactUserId
        };

        AddContactResult result = await _commandRepo.AddContact(userClaims.UserId, contact);
        if (result.Success)
        {
            _logger.LogTrace("Responding with a successful new request from user {UserId} to contact {ContactUserId}", userClaims.UserId, request.ContactUserId);
            return new InviteResponse
            {
                Success = true,
                Version = result.Version.ToUuid()
            };
        }
        if (result.AlreadyExists)
        {
            _logger.LogTrace("Responding with failed new request from user {UserId} to contact {ContactUserId} because the connection has already exists", userClaims.UserId, request.ContactUserId);
            return new InviteResponse
            {
                AlreadyExists = true
            };
        }

        throw new InvalidOperationException($"Failed to process {nameof(AddContactResult)}.");
    }

    public override async Task<ApproveResponse> Approve(ApproveRequest request, ServerCallContext context)
    {
        UserClaims userClaims = GetUserClaims(context);

        Contact? existingContact = await _queryRepo.GetContact(userClaims.UserId, request.ContactUserId);
        if (existingContact == null)
        {
            _logger.LogTrace("Responding with failed approval from user {UserId} to contact {ContactUserId} because the connection is missing", userClaims.UserId, request.ContactUserId);
            return new ApproveResponse
            {
                MissingContact = true
            };
        }
        if (existingContact.TargetUserId != userClaims.UserId || existingContact.Status != ContactStatus.Pending)
        {
            _logger.LogTrace("Responding with failed approval from user {UserId} to contact {ContactUserId} because of invalid target user or status", userClaims.UserId, request.ContactUserId);
            return new ApproveResponse
            {
                Invalid = true
            };
        }

        existingContact.Version = request.Version;
        existingContact.Status = ContactStatus.Connected;
        existingContact.TargetUserId = 0;

        UpdateContactResult result = await _commandRepo.UpdateContact(userClaims.UserId, existingContact);
        if (result.Success)
        {
            _logger.LogTrace("Responding with a successful approval from user {UserId} to contact {ContactUserId}", userClaims.UserId, request.ContactUserId);
            return new ApproveResponse
            {
                Success = true,
                NewVersion = result.NewVersion.ToUuid()
            };
        }
        if (result.ConcurrentlyUpdated)
        {
            _logger.LogTrace("Responding with a failed approval from user {UserId} to contact {ContactUserId}", userClaims.UserId, request.ContactUserId);
            return new ApproveResponse
            {
                ConcurrentlyUpdated = true
            };
        }

        throw new InvalidOperationException($"Failed to process {nameof(UpdateContactResult)}.");
    }

    public override async Task<CancelResponse> Cancel(CancelRequest request, ServerCallContext context)
    {
        UserClaims userClaims = GetUserClaims(context);

        Contact? existingContact = await _queryRepo.GetContact(userClaims.UserId, request.ContactUserId);
        if (existingContact == null)
        {
            _logger.LogTrace("Responding with failed cancel from user {UserId} to contact {ContactUserId} because the connection is missing", userClaims.UserId, request.ContactUserId);
            return new CancelResponse
            {
                MissingContact = true
            };
        }
        if (existingContact.Status != ContactStatus.Pending)
        {
            _logger.LogTrace("Responding with failed cancel from user {UserId} to contact {ContactUserId} because of invalid status", userClaims.UserId, request.ContactUserId);
            return new CancelResponse
            {
                Invalid = true
            };
        }

        existingContact.Version = request.Version;

        RemoveContactResult result = await _commandRepo.RemoveContact(userClaims.UserId, existingContact);
        if (result.Success)
        {
            _logger.LogTrace("Responding with a successful cancel from user {UserId} to contact {ContactUserId}", userClaims.UserId, request.ContactUserId);
            return new CancelResponse
            {
                Success = true
            };
        }
        if (result.ConcurrentlyUpdated)
        {
            _logger.LogTrace("Responding with a failed cancel from user {UserId} to contact {ContactUserId}", userClaims.UserId, request.ContactUserId);
            return new CancelResponse
            {
                ConcurrentlyUpdated = true
            };
        }

        throw new InvalidOperationException($"Failed to process {nameof(RemoveContactResult)}.");
    }

    public override async Task<RemoveResponse> Remove(RemoveRequest request, ServerCallContext context)
    {
        UserClaims userClaims = GetUserClaims(context);

        Contact? existingContact = await _queryRepo.GetContact(userClaims.UserId, request.ContactUserId);
        if (existingContact == null)
        {
            _logger.LogTrace("Responding with failed removal from user {UserId} to contact {ContactUserId} because the connection is missing", userClaims.UserId, request.ContactUserId);
            return new RemoveResponse
            {
                MissingContact = true
            };
        }
        if (existingContact.Status != ContactStatus.Connected)
        {
            _logger.LogTrace("Responding with a failed removal from user {UserId} to contact {ContactUserId} because of invalid status", userClaims.UserId, request.ContactUserId);
            return new RemoveResponse
            {
                Invalid = true
            };
        }

        existingContact.Version = request.Version;

        RemoveContactResult result = await _commandRepo.RemoveContact(userClaims.UserId, existingContact);
        if (result.Success)
        {
            _logger.LogTrace("Responding with a successful removal from user {UserId} to contact {ContactUserId}", userClaims.UserId, request.ContactUserId);
            return new RemoveResponse
            {
                Success = true
            };
        }
        if (result.ConcurrentlyUpdated)
        {
            _logger.LogTrace("Responding with a failed removal from user {UserId} to contact {ContactUserId}", userClaims.UserId, request.ContactUserId);
            return new RemoveResponse
            {
                ConcurrentlyUpdated = true
            };
        }

        throw new InvalidOperationException($"Failed to process {nameof(RemoveContactResult)}.");
    }

    // TODO: reuse
    private UserClaims GetUserClaims(ServerCallContext context)
    {
        if (!context.GetHttpContext().TryGetUserClaims(_logger, out UserClaims? userClaims))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, string.Empty));
        }

        return userClaims;
    }
}
