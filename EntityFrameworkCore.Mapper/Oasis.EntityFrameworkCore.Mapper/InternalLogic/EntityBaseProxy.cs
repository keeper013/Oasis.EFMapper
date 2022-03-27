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
    private readonly bool _defaultKeepEntityOnMappingRemoved;
    private readonly IReadOnlySet<Type> _keepEntityOnMappingRemovedTypes;
    private readonly IReadOnlySet<Type> _removeEntityOnMappingRemoveTypes;
    private readonly IReadOnlyDictionary<Type, TypeProxy> _proxies;
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, EntityComparer>> _comparers;
    private readonly IScalarTypeConverter _scalarConverter;

    public EntityBaseProxy(
        Dictionary<Type, TypeProxyMetaDataSet> proxies,
        Dictionary<Type, Dictionary<Type, ComparerMetaDataSet>> comparers,
        Type type,
        IScalarTypeConverter scalarConverter,
        bool defaultKeepEntityOnMappingRemoved)
    {
        _defaultKeepEntityOnMappingRemoved = defaultKeepEntityOnMappingRemoved;
        var typeProxies = new Dictionary<Type, TypeProxy>();
        var keepEntityOnMappingRemoved = new HashSet<Type>();
        var removeEntityOnMappingRemoved = new HashSet<Type>();
        foreach (var pair in proxies)
        {
            var typeMetaDataSet = pair.Value;
            var typeProxy = new TypeProxy(
                Delegate.CreateDelegate(typeMetaDataSet.getId.type, type!.GetMethod(typeMetaDataSet.getId.name)!),
                Delegate.CreateDelegate(typeMetaDataSet.identityIsEmpty.type, type!.GetMethod(typeMetaDataSet.identityIsEmpty.name)!),
                Delegate.CreateDelegate(typeMetaDataSet.timestampIsEmpty.type, type!.GetMethod(typeMetaDataSet.timestampIsEmpty.name)!),
                typeMetaDataSet.identityProperty);
            typeProxies.Add(pair.Key, typeProxy);
            if (typeMetaDataSet.keepEntityOnMappingRemoved)
            {
                keepEntityOnMappingRemoved.Add(pair.Key);
            }
            else
            {
                removeEntityOnMappingRemoved.Add(pair.Key);
            }
        }

        _keepEntityOnMappingRemovedTypes = keepEntityOnMappingRemoved;
        _removeEntityOnMappingRemoveTypes = removeEntityOnMappingRemoved;
        _proxies = typeProxies;

        var comparer = new Dictionary<Type, IReadOnlyDictionary<Type, EntityComparer>>();
        foreach (var pair in comparers)
        {
            var innerDictionary = new Dictionary<Type, EntityComparer>();
            foreach (var innerPair in pair.Value)
            {
                var comparerMetaDataSet = innerPair.Value;
                var comparerSet = new EntityComparer(
                    Delegate.CreateDelegate(comparerMetaDataSet.identityComparer.type, type!.GetMethod(comparerMetaDataSet.identityComparer.name)!),
                    Delegate.CreateDelegate(comparerMetaDataSet.timeStampComparer.type, type!.GetMethod(comparerMetaDataSet.timeStampComparer.name)!));
                innerDictionary.Add(innerPair.Key, comparerSet);
            }

            comparer.Add(pair.Key, innerDictionary);
        }

        _comparers = comparer;

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
        if (_defaultKeepEntityOnMappingRemoved ?
            _removeEntityOnMappingRemoveTypes.Contains(typeof(TEntity)) :
            !_keepEntityOnMappingRemovedTypes.Contains(typeof(TEntity)))
        {
            databaseContext.Set<TEntity>().Remove(entity);
        }
    }

    public PropertyInfo GetIdProperty<TEntity>()
    {
        if (_proxies.TryGetValue(typeof(TEntity), out var proxy))
        {
            return proxy.identityProperty;
        }

        throw new TypeNotProperlyRegisteredException(typeof(TEntity));
    }
}
