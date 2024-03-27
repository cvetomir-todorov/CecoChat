using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CecoChat.User.Data.Infra;

internal interface IDataUtility
{
    DateTime SetVersion<TEntity>(EntityEntry<TEntity> entity, DateTime originalVersion)
        where TEntity : class, IVersionEntity;
}

internal class DataUtility : IDataUtility
{
    public DateTime SetVersion<TEntity>(EntityEntry<TEntity> entity, DateTime originalVersion)
        where TEntity : class, IVersionEntity
    {
        PropertyEntry<TEntity, DateTime> property = entity.Property(e => e.Version);
        property.OriginalValue = originalVersion;
        property.CurrentValue = DateTime.UtcNow;

        return property.CurrentValue;
    }
}
