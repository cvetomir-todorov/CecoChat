using CecoChat.Contracts;
using CecoChat.Contracts.User;
using CecoChat.Data.User.Infra;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CecoChat.Data.User.Contacts;

internal class ContactCommandRepo : IContactCommandRepo
{
    private readonly ILogger _logger;
    private readonly UserDbContext _dbContext;
    private readonly IDataUtility _dataUtility;

    public ContactCommandRepo(
        ILogger<ContactCommandRepo> logger,
        UserDbContext dbContext,
        IDataUtility dataUtility)
    {
        _logger = logger;
        _dbContext = dbContext;
        _dataUtility = dataUtility;
    }

    public async Task<AddContactResult> AddContact(long userId, Contact contact)
    {
        ContactEntity entity = CreateEntityWithUserIds(userId, contact.ContactUserId);
        entity.Version = Guid.NewGuid();
        entity.Status = MapStatus(contact.Status);
        entity.TargetUserId = contact.TargetUserId;

        _dbContext.Contacts.Add(entity);

        try
        {
            await _dbContext.SaveChangesAsync();
            _logger.LogTrace("Added a new contact for user {UserId} and contact {ContactUserId}", userId, contact.ContactUserId);

            return new AddContactResult
            {
                Success = true,
                Version = entity.Version
            };
        }
        catch (DbUpdateException dbUpdateException) when (dbUpdateException.InnerException is PostgresException postgresException)
        {
            // https://www.postgresql.org/docs/current/errcodes-appendix.html
            if (postgresException.SqlState == "23505")
            {
                if (postgresException.MessageText.Contains("Contacts_pkey"))
                {
                    return new AddContactResult
                    {
                        AlreadyExists = true
                    };
                }
            }

            throw;
        }
    }

    public async Task<UpdateContactResult> UpdateContact(long userId, Contact contact)
    {
        ContactEntity entity = CreateEntityWithUserIds(userId, contact.ContactUserId);
        entity.Status = MapStatus(contact.Status);
        entity.TargetUserId = contact.TargetUserId;

        EntityEntry<ContactEntity> entry = _dbContext.Contacts.Attach(entity);
        entry.Property(e => e.Status).IsModified = true;
        entry.Property(e => e.TargetUserId).IsModified = true;
        Guid newVersion = _dataUtility.SetVersion(entry, contact.Version.ToGuid());

        try
        {
            await _dbContext.SaveChangesAsync();
            _logger.LogTrace("Updated existing contact {ContactUserId} for user {UserId} with status {ContactStatus}", contact.ContactUserId, userId, contact.Status);

            return new UpdateContactResult
            {
                Success = true,
                NewVersion = newVersion
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            return new UpdateContactResult
            {
                ConcurrentlyUpdated = true
            };
        }
    }

    public async Task<RemoveContactResult> RemoveContact(long userId, Contact contact)
    {
        ContactEntity entity = CreateEntityWithUserIds(userId, contact.ContactUserId);

        EntityEntry<ContactEntity> entry = _dbContext.Contacts.Attach(entity);
        entry.State = EntityState.Deleted;
        _dataUtility.SetVersion(entry, contact.Version.ToGuid());

        try
        {
            await _dbContext.SaveChangesAsync();
            _logger.LogTrace("Removed existing contact {ContactUserId} for user {UserId}", contact.ContactUserId, userId);

            return new RemoveContactResult
            {
                Success = true
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            return new RemoveContactResult
            {
                ConcurrentlyUpdated = true
            };
        }
    }

    private static ContactEntity CreateEntityWithUserIds(long userId, long contactUserId)
    {
        if (userId == contactUserId)
        {
            throw new InvalidOperationException($"User ID and contact user ID should be different, but have the same value {userId}.");
        }

        long smallerId = Math.Min(userId, contactUserId);
        long biggerId = Math.Max(userId, contactUserId);

        return new ContactEntity
        {
            User1Id = smallerId,
            User2Id = biggerId
        };
    }

    private static ContactEntityStatus MapStatus(ContactStatus status)
    {
        switch (status)
        {
            case ContactStatus.Pending:
                return ContactEntityStatus.Pending;
            case ContactStatus.Connected:
                return ContactEntityStatus.Connected;
            default:
                throw new EnumValueNotSupportedException(status);
        }
    }
}
