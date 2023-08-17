using CecoChat.Contracts.User;

namespace CecoChat.Data.User.Contacts;

public interface IContactQueryRepo
{
    Task<Contact?> GetContact(long userId, long contactUserId);

    Task<IEnumerable<Contact>> GetContacts(long userId);
}
