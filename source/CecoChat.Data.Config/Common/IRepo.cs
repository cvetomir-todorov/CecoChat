namespace CecoChat.Data.Config.Common;

internal interface IRepo<TValues>
    where TValues: class
{
    Task<TValues> Load();
}
