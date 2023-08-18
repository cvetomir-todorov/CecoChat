using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CecoChat.Data.User.Infra;

internal interface IDataUtility
{
    Guid SetVersion<TEntity>(EntityEntry<TEntity> entity, Guid originalVersion)
        where TEntity : class, IVersionEntity;
}

internal class DataUtility : IDataUtility
{
    public Guid SetVersion<TEntity>(EntityEntry<TEntity> entity, Guid originalVersion)
        where TEntity : class, IVersionEntity
    {
        PropertyEntry<TEntity, Guid> property = entity.Property(e => e.Version);
        property.OriginalValue = originalVersion;
        property.CurrentValue = Guid.NewGuid();

        return property.CurrentValue;
    }
}
