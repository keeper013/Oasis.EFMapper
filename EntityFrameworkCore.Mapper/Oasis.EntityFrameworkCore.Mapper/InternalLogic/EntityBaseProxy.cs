namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Microsoft.EntityFrameworkCore;

internal sealed class EntityBaseProxy
{
    private readonly Dictionary<Type, TypeConfiguration> _configurations;

    public EntityBaseProxy(Dictionary<Type, TypeConfiguration> configurations)
    {
        _configurations = configurations;
    }

    //object GetId<TEntity>(TEntity entity)
    //    where TEntity : class;

    //bool IdIsEmpty<TEntity>(TEntity entity)
    //    where TEntity : class;

    //bool TimeStampIsEmpty<TEntity>(TEntity entity)
    //    where TEntity : class;

    //bool IdEquals<TEntity1, TEntity2>(TEntity1 entity1, TEntity2 entity2)
    //    where TEntity1 : class
    //    where TEntity2 : class;

    //bool TimeStampEquals<TEntity1, TEntity2>(TEntity1 entity1, TEntity2 entity2)
    //    where TEntity1 : class
    //    where TEntity2 : class;

    public void HandleRemove<TEntity>(DbContext databaseContext, TEntity entity)
        where TEntity : class
    {
        if (!(_configurations.TryGetValue(typeof(TEntity), out var config) && config.keepEntityOnMappingRemoved))
        {
            databaseContext.Set<TEntity>().Remove(entity);
        }
    }
}
