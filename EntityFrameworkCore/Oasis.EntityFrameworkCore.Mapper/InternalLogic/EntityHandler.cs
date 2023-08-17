namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Linq.Expressions;

internal interface IIdPropertyTracker
{
    PropertyInfo GetIdProperty<TEntity>();
}

/// <summary>
/// This class stores generated functions that handles entity id and concurrency token related matters.
/// </summary>
internal sealed class EntityHandler : IIdPropertyTracker
{
    private readonly IReadOnlyDictionary<Type, TypeKeyProxy> _entityIdProxies;
    private readonly IReadOnlyDictionary<Type, TypeKeyProxy> _entityConcurrencyTokenProxies;
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> _entityIdComparers;
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> _entityConcurrencyTokenComparers;
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> _targetIdEqualsSourceId;
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> _sourceIdListContainsTargetId;
    private readonly IReadOnlyDictionary<Type, Delegate> _factoryMethods;
    private readonly IScalarTypeConverter _scalarTypeConverter;

    public EntityHandler(
        Dictionary<Type, TypeKeyProxyMetaDataSet> entityIdProxies,
        Dictionary<Type, TypeKeyProxyMetaDataSet> entityConcurrencyTokenProxies,
        Dictionary<Type, Dictionary<Type, MethodMetaData>> entityIdComparers,
        Dictionary<Type, Dictionary<Type, MethodMetaData>> entityConcurrencyTokenComparers,
        Dictionary<Type, Dictionary<Type, MethodMetaData>> sourceIdForTarget,
        Dictionary<Type, Dictionary<Type, MethodMetaData>> sourceIdListContainsTargetId,
        Dictionary<Type, Delegate> factoryMethods,
        Dictionary<Type, MethodMetaData> generatedConstructors,
        Type type,
        IScalarTypeConverter scalarConverter)
    {
        _entityIdProxies = MakeTypeKeyProxyDictionary(entityIdProxies, type);
        _entityConcurrencyTokenProxies = MakeTypeKeyProxyDictionary(entityConcurrencyTokenProxies, type);
        _entityIdComparers = Utilities.MakeDelegateDictionary(entityIdComparers, type);
        _entityConcurrencyTokenComparers = Utilities.MakeDelegateDictionary(entityConcurrencyTokenComparers, type);
        _targetIdEqualsSourceId = Utilities.MakeDelegateDictionary(sourceIdForTarget, type);
        _sourceIdListContainsTargetId = Utilities.MakeDelegateDictionary(sourceIdListContainsTargetId, type);
        var dict = new Dictionary<Type, Delegate>(factoryMethods);
        foreach (var kvp in generatedConstructors)
        {
            dict.Add(kvp.Key, Delegate.CreateDelegate(kvp.Value.type, type.GetMethod(kvp.Value.name)!));
        }

        _factoryMethods = dict;
        _scalarTypeConverter = scalarConverter;
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
            return ((Utilities.ScalarPropertiesAreEqual<TSource, TTarget>)areEqual)(source, target, _scalarTypeConverter);
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
            return ((Utilities.ScalarPropertiesAreEqual<TSource, TTarget>)areEqual)(source, target, _scalarTypeConverter);
        }

        throw new KeyPropertyMissingException(sourceType, targetType, "concurrency token");
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

    public Expression<Func<TTarget, bool>> GetContainsTargetIdExpression<TSource, TTarget>(List<TSource> sourceList)
        where TSource : class
        where TTarget : class
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);
        var del = _sourceIdListContainsTargetId.Find(sourceType, targetType);
        if (del == null)
        {
            throw new InvalidOperationException($"Missing source id list contains target id method when mapping from {sourceType.Name} to {targetType.Name}");
        }

        return ((Utilities.GetSourceIdListContainsTargetId<TSource, TTarget>)del)(sourceList, _scalarTypeConverter);
    }

    public Expression<Func<TTarget, bool>> GetIdEqualsExpression<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);
        var del = _targetIdEqualsSourceId.Find(sourceType, targetType);
        if (del == null)
        {
            throw new InvalidOperationException($"Missing source id for target method when mapping from {sourceType.Name} to {targetType.Name}");
        }

        return ((Utilities.GetSourceIdEqualsTargetId<TSource, TTarget>)del)(source, _scalarTypeConverter);
    }

    public TEntity Make<TEntity>()
        where TEntity : class
    {
        return ((Func<TEntity>)_factoryMethods[typeof(TEntity)])();
    }

    private Dictionary<Type, TypeKeyProxy> MakeTypeKeyProxyDictionary(Dictionary<Type, TypeKeyProxyMetaDataSet> proxies, Type type)
    {
        var result = new Dictionary<Type, TypeKeyProxy>();
        foreach (var pair in proxies)
        {
            var typeKeyDataSet = pair.Value;
            var proxy = new TypeKeyProxy(
                Delegate.CreateDelegate(typeKeyDataSet.isEmpty.type, type.GetMethod(typeKeyDataSet.isEmpty.name)!),
                typeKeyDataSet.property);
            result.Add(pair.Key, proxy);
        }

        return result;
    }
}
