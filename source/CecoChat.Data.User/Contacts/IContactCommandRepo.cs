using CecoChat.Contracts.User;

namespace CecoChat.Data.User.Contacts;

public interface IContactCommandRepo
{
    Task<AddContactResult> AddContact(long userId, Contact contact);

    Task<UpdateContactResult> UpdateContact(long userId, Contact contact);

    Task<RemoveContactResult> RemoveContact(long userId, Contact contact);
}

public readonly struct AddContactResult
{
    public bool Success { get; init; }
    public Guid Version { get; init; }
    public bool AlreadyExists { get; init; }
}

public readonly struct UpdateContactResult
{
    public bool Success { get; init; }
    public Guid NewVersion { get; init; }
    public bool ConcurrentlyUpdated { get; init; }
}

public readonly struct RemoveContactResult
{
    public bool Success { get; init; }
    public bool ConcurrentlyUpdated { get; init; }
}
