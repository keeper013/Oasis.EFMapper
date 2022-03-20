namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Reflection;

internal interface IIdPropertyTracker
{
    PropertyInfo GetIdProperty<TEntity>();
}

internal sealed class EntityBaseProxy : IIdPropertyTracker
{
    private static readonly IReadOnlyDictionary<Type, object> DefaultIdValues = new Dictionary<Type, object>
    {
        { typeof(int), Activator.CreateInstance(typeof(int))! },
        { typeof(long), Activator.CreateInstance(typeof(long))! },
        { typeof(uint), Activator.CreateInstance(typeof(uint))! },
        { typeof(ulong), Activator.CreateInstance(typeof(ulong))! },
        { typeof(short), Activator.CreateInstance(typeof(short))! },
        { typeof(ushort), Activator.CreateInstance(typeof(ushort))! },
        { typeof(byte), Activator.CreateInstance(typeof(byte))! },
    };

    private static readonly IReadOnlyDictionary<Type, object> DefaultTimeStampValues = new Dictionary<Type, object>
    {
        { typeof(DateTime), Activator.CreateInstance(typeof(DateTime))! },
        { typeof(int), Activator.CreateInstance(typeof(int))! },
        { typeof(long), Activator.CreateInstance(typeof(long))! },
        { typeof(uint), Activator.CreateInstance(typeof(uint))! },
        { typeof(ulong), Activator.CreateInstance(typeof(ulong))! },
        { typeof(short), Activator.CreateInstance(typeof(short))! },
        { typeof(ushort), Activator.CreateInstance(typeof(ushort))! },
        { typeof(byte), Activator.CreateInstance(typeof(byte))! },
    };

    private readonly IReadOnlyDictionary<Type, TypeProxy> _proxies;
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, EntityComparer>> _comparers;
    private readonly IScalarTypeConverter _scalarConverter;

    public EntityBaseProxy(
        IReadOnlyDictionary<Type, TypeProxy> proxies,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, EntityComparer>> comparers,
        IScalarTypeConverter scalarConverter)
    {
        _proxies = proxies;
        _comparers = comparers;
        _scalarConverter = scalarConverter;
    }

    public object GetId<TEntity>(TEntity entity)
        where TEntity : class
    {
        return ((Utilities.GetId<TEntity>)_proxies[typeof(TEntity)].getId)(entity);
    }

    public bool IdIsEmpty<TEntity>(TEntity entity)
        where TEntity : class
    {
        return ((Utilities.IdIsEmpty<TEntity>)_proxies[typeof(TEntity)].identityIsEmpty)(entity);
    }

    public bool TimeStampIsEmpty<TEntity>(TEntity entity)
        where TEntity : class
    {
        return ((Utilities.TimeStampIsEmpty<TEntity>)_proxies[typeof(TEntity)].timestampIsEmpty)(entity);
    }

    public bool IdEquals<TSource, TTarget>(TSource source, TTarget target)
        where TSource : class
        where TTarget : class
    {
        return ((Utilities.IdsAreEqual<TSource, TTarget>)_comparers[typeof(TSource)][typeof(TTarget)].idsAreEqual)(source, target, _scalarConverter);
    }

    public bool TimeStampEquals<TSource, TTarget>(TSource source, TTarget target)
        where TSource : class
        where TTarget : class
    {
        return ((Utilities.TimeStampsAreEqual<TSource, TTarget>)_comparers[typeof(TSource)][typeof(TTarget)].timestampsAreEqual)(source, target, _scalarConverter);
    }

    public void HandleRemove<TEntity>(DbContext databaseContext, TEntity entity)
        where TEntity : class
    {
        if (!(_proxies.TryGetValue(typeof(TEntity), out var config) && config.keepEntityOnMappingRemoved))
        {
            databaseContext.Set<TEntity>().Remove(entity);
        }
    }

    public PropertyInfo GetIdProperty<TEntity>()
    {
        if (_proxies.TryGetValue(typeof(TEntity), out var proxy))
        {
            return proxy.GetIdProperty();
        }

        throw new TypeNotProperlyRegisteredException(typeof(TEntity));
    }
}
