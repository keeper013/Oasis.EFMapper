namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Exceptions;

internal interface IIdPropertyTracker
{
    PropertyInfo GetIdProperty<TEntity>();
}

internal sealed class EntityBaseProxy : IIdPropertyTracker
{
    private readonly bool _defaultKeepEntityOnMappingRemoved;
    private readonly IReadOnlyDictionary<Type, TypeKeyProxy> _entityIdProxies;
    private readonly IReadOnlyDictionary<Type, TypeKeyProxy> _entityConcurrencyTokenProxies;
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> _entityIdComparers;
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> _entityConcurrencyTokenComparers;
    private readonly IScalarTypeConverter _scalarConverter;

    public EntityBaseProxy(
        Dictionary<Type, TypeKeyProxyMetaDataSet> entityIdProxies,
        Dictionary<Type, TypeKeyProxyMetaDataSet> entityConcurrencyTokenProxies,
        Dictionary<Type, Dictionary<Type, MethodMetaData>> entityIdComparers,
        Dictionary<Type, Dictionary<Type, MethodMetaData>> entityConcurrencyTokenComparers,
        Type type,
        IScalarTypeConverter scalarConverter,
        bool defaultKeepEntityOnMappingRemoved)
    {
        _defaultKeepEntityOnMappingRemoved = defaultKeepEntityOnMappingRemoved;
        _entityIdProxies = MakeTypeKeyProxyDictionary(entityIdProxies, type);
        _entityConcurrencyTokenProxies = MakeTypeKeyProxyDictionary(entityConcurrencyTokenProxies, type);
        _entityIdComparers = MakeComparerDictionary(entityIdComparers, type);
        _entityConcurrencyTokenComparers = MakeComparerDictionary(entityConcurrencyTokenComparers, type);

        _scalarConverter = scalarConverter;
    }

    public bool HasId<TEntity>()
        where TEntity : class
    {
        return _entityIdProxies.ContainsKey(typeof(TEntity));
    }

    public bool HasConcurrencyToken<TEntity>()
        where TEntity : class
    {
        return _entityConcurrencyTokenProxies.ContainsKey(typeof(TEntity));
    }

    public object GetId<TEntity>(TEntity entity)
        where TEntity : class
    {
        var type = typeof(TEntity);
        if (_entityIdProxies.TryGetValue(type, out var value))
        {
            return ((Utilities.GetScalarProperty<TEntity>)value.get!)(entity);
        }

        throw new KeyPropertyMissingException(type, "identity");
    }

    public bool IdIsEmpty<TEntity>(TEntity entity)
        where TEntity : class
    {
        var type = typeof(TEntity);
        if (_entityIdProxies.TryGetValue(type, out var value))
        {
            return ((Utilities.ScalarPropertyIsEmpty<TEntity>)value.isEmpty)(entity);
        }

        throw new KeyPropertyMissingException(type, "identity");
    }

    public bool ConcurrencyTokenIsEmpty<TEntity>(TEntity entity)
        where TEntity : class
    {
        var type = typeof(TEntity);
        if (_entityConcurrencyTokenProxies.TryGetValue(type, out var value))
        {
            return ((Utilities.ScalarPropertyIsEmpty<TEntity>)value.isEmpty)(entity);
        }

        throw new KeyPropertyMissingException(type, "concurrenty token");
    }

    public bool IdEquals<TSource, TTarget>(TSource source, TTarget target)
        where TSource : class
        where TTarget : class
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);
        if (_entityIdComparers.TryGetValue(sourceType, out var inner) && inner.TryGetValue(targetType, out var areEqual))
        {
            return ((Utilities.ScalarPropertiesAreEqual<TSource, TTarget>)areEqual)(source, target, _scalarConverter);
        }

        throw new KeyPropertyMissingException(sourceType, targetType, "identity");
    }

    public bool ConcurrencyTokenEquals<TSource, TTarget>(TSource source, TTarget target)
        where TSource : class
        where TTarget : class
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);
        if (_entityConcurrencyTokenComparers.TryGetValue(sourceType, out var inner) && inner.TryGetValue(targetType, out var areEqual))
        {
            return ((Utilities.ScalarPropertiesAreEqual<TSource, TTarget>)areEqual)(source, target, _scalarConverter);
        }

        throw new KeyPropertyMissingException(sourceType, targetType, "concurrency token");
    }

    public void HandleRemove<TEntity>(DbContext databaseContext, TEntity entity)
        where TEntity : class
    {
        var type = typeof(TEntity);
        if (!_defaultKeepEntityOnMappingRemoved)
        {
            databaseContext.Set<TEntity>().Remove(entity);
        }
    }

    public PropertyInfo GetIdProperty<TEntity>()
    {
        var type = typeof(TEntity);
        if (_entityIdProxies.TryGetValue(type, out var proxy))
        {
            return proxy.property;
        }

        throw new KeyPropertyMissingException(type, "identity");
    }

    private Dictionary<Type, TypeKeyProxy> MakeTypeKeyProxyDictionary(Dictionary<Type, TypeKeyProxyMetaDataSet> proxies, Type type)
    {
        var result = new Dictionary<Type, TypeKeyProxy>();
        foreach (var pair in proxies)
        {
            var typeKeyDataSet = pair.Value;
            var proxy = new TypeKeyProxy(
                typeKeyDataSet.get.HasValue ? Delegate.CreateDelegate(typeKeyDataSet.get.Value.type, type.GetMethod(typeKeyDataSet.get.Value.name)!) : default,
                Delegate.CreateDelegate(typeKeyDataSet.isEmpty.type, type.GetMethod(typeKeyDataSet.isEmpty.name)!),
                typeKeyDataSet.property);
            result.Add(pair.Key, proxy);
        }

        return result;
    }

    private Dictionary<Type, IReadOnlyDictionary<Type, Delegate>> MakeComparerDictionary(IDictionary<Type, Dictionary<Type, MethodMetaData>> comparers, Type type)
    {
        var result = new Dictionary<Type, IReadOnlyDictionary<Type, Delegate>>();
        foreach (var pair in comparers)
        {
            var innerDictionary = new Dictionary<Type, Delegate>();
            foreach (var innerPair in pair.Value)
            {
                var comparer = innerPair.Value;
                var @delegate = Delegate.CreateDelegate(comparer.type, type.GetMethod(comparer.name)!);
                innerDictionary.Add(innerPair.Key, @delegate);
            }

            result.Add(pair.Key, innerDictionary);
        }

        return result;
    }
}
