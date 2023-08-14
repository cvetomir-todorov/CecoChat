using AutoMapper;
using CecoChat.Contracts;
using CecoChat.Contracts.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.User.Contacts;

public interface IContactQueryRepo
{
    Task<IEnumerable<Contact>> GetContacts(long userId);
}

internal class ContactQueryRepo : IContactQueryRepo
{
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly UserDbContext _dbContext;

    public ContactQueryRepo(
        ILogger<ContactQueryRepo> logger,
        IMapper mapper,
        UserDbContext dbContext)
    {
        _logger = logger;
        _mapper = mapper;
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Contact>> GetContacts(long userId)
    {
        List<Contact> contacts = await _dbContext.Contacts
            .Where(entity => entity.User1Id == userId)
            .Union(_dbContext.Contacts.Where(entity => entity.User2Id == userId))
            .Select(entity =>
                new Contact
                {
                    ContactUserId = entity.User1Id == userId ? entity.User2Id : entity.User1Id,
                    Version = entity.Version.ToUuid(),
                    Status = Map(entity.Status)
                }
            )
            .ToListAsync();

        _logger.LogTrace("Fetched {ContactCount} contacts for user {UserId}", contacts.Count, userId);
        return contacts;
    }

    private static ContactStatus Map(ContactEntityStatus entityStatus)
    {
        switch (entityStatus)
        {
            case ContactEntityStatus.PendingRequest:
                return ContactStatus.PendingRequest;
            case ContactEntityStatus.CancelledRequest:
                return ContactStatus.CancelledRequest;
            case ContactEntityStatus.Established:
                return ContactStatus.Established;
            case ContactEntityStatus.Removed:
                return ContactStatus.Removed;
            default:
                throw new EnumValueNotSupportedException(entityStatus);
        }
    }
}
