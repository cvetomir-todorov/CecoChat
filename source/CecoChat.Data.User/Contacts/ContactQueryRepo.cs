using CecoChat.Contracts;
using CecoChat.Contracts.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.User.Contacts;

internal class ContactQueryRepo : IContactQueryRepo
{
    private readonly ILogger _logger;
    private readonly UserDbContext _dbContext;

    public ContactQueryRepo(
        ILogger<ContactQueryRepo> logger,
        UserDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<Contact?> GetContact(long userId, long contactUserId)
    {
        long smallerId = Math.Min(userId, contactUserId);
        long biggerId = Math.Max(userId, contactUserId);

        ContactEntity? entity = await _dbContext.Contacts
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.User1Id == smallerId && entity.User2Id == biggerId);

        if (entity == null)
        {
            _logger.LogTrace("Failed to fetch contact {ContactUserId} for user {UserId}", contactUserId, userId);
            return null;
        }

        _logger.LogTrace("Fetched contact {ContactUserId} for user {UserId}", contactUserId, userId);
        return MapContact(userId, entity);
    }

    public async Task<IEnumerable<Contact>> GetContacts(long userId)
    {
        List<Contact> contacts = await _dbContext.Contacts
            .Where(entity => entity.User1Id == userId)
            .Union(_dbContext.Contacts.Where(entity => entity.User2Id == userId))
            .Select(entity => MapContact(userId, entity))
            .AsNoTracking()
            .ToListAsync();

        _logger.LogTrace("Fetched {ContactCount} contacts for user {UserId}", contacts.Count, userId);
        return contacts;
    }

    private static Contact MapContact(long userId, ContactEntity entity)
    {
        return new Contact
        {
            ContactUserId = MapContactUserId(userId, entity),
            Version = entity.Version.ToUuid(),
            Status = MapStatus(entity.Status),
            TargetUserId = entity.TargetUserId
        };
    }

    private static long MapContactUserId(long userId, ContactEntity entity)
    {
        return entity.User1Id == userId ?
            entity.User2Id :
            entity.User1Id;
    }

    private static ContactStatus MapStatus(ContactEntityStatus entityStatus)
    {
        switch (entityStatus)
        {
            case ContactEntityStatus.Pending:
                return ContactStatus.Pending;
            case ContactEntityStatus.Connected:
                return ContactStatus.Connected;
            default:
                throw new EnumValueNotSupportedException(entityStatus);
        }
    }
}
