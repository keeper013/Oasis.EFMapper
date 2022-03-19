namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Microsoft.EntityFrameworkCore;

internal interface IIdPropertyNameTracker
{
    string GetIdPropertyName<TEntity>();
}

internal sealed class EntityBaseProxy : IIdPropertyNameTracker
{
    private readonly IReadOnlyDictionary<Type, TypeConfiguration> _configurations;

    public EntityBaseProxy(IReadOnlyDictionary<Type, TypeConfiguration> configurations)
    {
        _configurations = configurations;
    }

    public object GetId<TEntity>(TEntity entity)
        where TEntity : class
    {
        throw new NotImplementedException();
    }

    public bool IdIsEmpty<TEntity>(TEntity entity)
        where TEntity : class
    {
        throw new NotImplementedException();
    }

    public bool TimeStampIsEmpty<TEntity>(TEntity entity)
        where TEntity : class
    {
        throw new NotImplementedException();
    }

    public bool IdEquals<TEntity1, TEntity2>(TEntity1 entity1, TEntity2 entity2)
        where TEntity1 : class
        where TEntity2 : class
    {
        throw new NotImplementedException();
    }

    public bool TimeStampEquals<TEntity1, TEntity2>(TEntity1 entity1, TEntity2 entity2)
        where TEntity1 : class
        where TEntity2 : class
    {
        throw new NotImplementedException();
    }

    public void HandleRemove<TEntity>(DbContext databaseContext, TEntity entity)
        where TEntity : class
    {
        if (!(_configurations.TryGetValue(typeof(TEntity), out var config) && config.keepEntityOnMappingRemoved))
        {
            databaseContext.Set<TEntity>().Remove(entity);
        }
    }

    public string GetIdPropertyName<TEntity>()
    {
        return _configurations.TryGetValue(typeof(TEntity), out var entity) ? entity.GetIdPropertyname() : Utilities.DefaultIdPropertyName;
    }
}
