namespace Oasis.EntityFramework.Mapper.InternalLogic;

using Oasis.EntityFramework.Mapper.Exceptions;
using System.Data.Entity;

internal interface IIdPropertyTracker
{
    PropertyInfo GetIdProperty<TEntity>();
}

internal sealed class EntityBaseProxy : IIdPropertyTracker
{
    private readonly bool _defaultKeepEntityOnMappingRemoved;
    private readonly ISet<Type> _keepEntityOnMappingRemovedTypes;
    private readonly ISet<Type> _removeEntityOnMappingRemoveTypes;
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
                    Delegate.CreateDelegate(comparerMetaDataSet.identityComparer.type, type!.GetMethod(comparerMetaDataSet.identityComparer.name)!));
                innerDictionary.Add(innerPair.Key, comparerSet);
            }

            comparer.Add(pair.Key, innerDictionary);
        }

        _comparers = comparer;

        _scalarConverter = scalarConverter;
    }

    public bool HasId<TEntity>()
        where TEntity : class
    {
        return _proxies.ContainsKey(typeof(TEntity));
    }

    public object GetId<TEntity>(TEntity entity)
        where TEntity : class
    {
        var type = typeof(TEntity);
        if (_proxies.TryGetValue(type, out var value))
        {
            return ((Utilities.GetId<TEntity>)value.getId)(entity);
        }

        throw new IdentityPropertyMissingException(type);
    }

    public bool IdIsEmpty<TEntity>(TEntity entity)
        where TEntity : class
    {
        var type = typeof(TEntity);
        if (_proxies.TryGetValue(type, out var value))
        {
            return ((Utilities.IdIsEmpty<TEntity>)value.identityIsEmpty)(entity);
        }

        throw new IdentityPropertyMissingException(type);
    }

    public bool IdEquals<TSource, TTarget>(TSource source, TTarget target)
        where TSource : class
        where TTarget : class
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);
        if (_comparers.TryGetValue(sourceType, out var inner) && inner.TryGetValue(targetType, out var value))
        {
            return ((Utilities.IdsAreEqual<TSource, TTarget>)value.idsAreEqual)(source, target, _scalarConverter);
        }

        throw new IdentityPropertyMissingException(sourceType, targetType);
    }

    public void HandleRemove<TEntity>(DbContext databaseContext, TEntity entity)
        where TEntity : class
    {
        var type = typeof(TEntity);
        if (_defaultKeepEntityOnMappingRemoved ?
            _removeEntityOnMappingRemoveTypes.Contains(type) :
            !_keepEntityOnMappingRemovedTypes.Contains(type))
        {
            databaseContext.Set<TEntity>().Remove(entity);
        }
    }

    public PropertyInfo GetIdProperty<TEntity>()
    {
        var type = typeof(TEntity);
        if (_proxies.TryGetValue(type, out var proxy))
        {
            return proxy.identityProperty;
        }

        throw new IdentityPropertyMissingException(type);
    }
}
