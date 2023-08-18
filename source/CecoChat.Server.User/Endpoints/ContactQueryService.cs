using CecoChat.Contracts.User;
using CecoChat.Data.User.Contacts;
using CecoChat.Server.Identity;
using Grpc.Core;

namespace CecoChat.Server.User.Endpoints;

public class ContactQueryService : ContactQuery.ContactQueryBase
{
    private readonly ILogger _logger;
    private readonly IContactQueryRepo _repo;

    public ContactQueryService(ILogger<ContactQueryService> logger, IContactQueryRepo repo)
    {
        _logger = logger;
        _repo = repo;
    }

    public override async Task<GetContactsResponse> GetContacts(GetContactsRequest request, ServerCallContext context)
    {
        UserClaims userClaims = context.GetUserClaimsGrpc(_logger);

        IEnumerable<Contact> contacts = await _repo.GetContacts(userClaims.UserId);

        GetContactsResponse response = new();
        response.Contacts.AddRange(contacts);

        _logger.LogTrace("Responding with {ContactCount} contacts for user {UserId}", response.Contacts.Count, userClaims.UserId);
        return response;
    }
}
