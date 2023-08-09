namespace Oasis.EntityFramework.Mapper.InternalLogic;

using System.Linq.Expressions;

internal static class Utilities
{
    public const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

    public delegate void MapKeyProperties<TSource, TTarget>(TSource source, TTarget target, IScalarTypeConverter converter, bool mapIdOnly)
        where TSource : class
        where TTarget : class;

    public delegate void MapProperties<TSource, TTarget, TKeyType>(
        TSource source,
        TTarget target,
        IScalarTypeConverter converter,
        IRecursiveMapper<TKeyType> mapper,
        IExistingTargetTracker? existingTargetTracker,
        INewTargetTracker<TKeyType>? newTargetTracker,
        bool? keepUnmatched)
        where TSource : class
        where TTarget : class
        where TKeyType : struct;

    public delegate object GetScalarProperty<TEntity>(TEntity entity)
        where TEntity : class;

    public delegate bool ScalarPropertyIsEmpty<TEntity>(TEntity entity)
        where TEntity : class;

    public delegate bool ScalarPropertiesAreEqual<TSource, TTarget>(TSource source, TTarget target, IScalarTypeConverter converter)
        where TSource : class
        where TTarget : class;

    public delegate bool StartTrackingNewTarget<TTarget, TKey>(ISet<TKey> set, TTarget target)
        where TTarget : class;

    public delegate object? GetSourceIdForTarget<TSource>(TSource source, IScalarTypeConverter converter)
        where TSource : class;

    public delegate Expression<Func<TTarget, bool>> GetSourceIdListContainsTargetId<TSource, TTarget>(List<TSource> sourceList, IScalarTypeConverter converter)
        where TSource : class
        where TTarget : class;

    public delegate Expression<Func<TTarget, bool>> GetSourceIdEqualsTargetId<TSource, TTarget>(TSource source, IScalarTypeConverter converter)
        where TSource : class
        where TTarget : class;

    public delegate IExistingTargetTracker BuildExistingTargetTracker(Delegate startTracking);

    public static bool AllowsInsert(this MapToDatabaseType mapToDatabaseType) => (mapToDatabaseType & MapToDatabaseType.Insert) == MapToDatabaseType.Insert;

    public static bool AllowsUpdate(this MapToDatabaseType mapToDatabaseType) => (mapToDatabaseType & MapToDatabaseType.Update) == MapToDatabaseType.Update;

    public static PropertyInfo? GetKeyProperty(this IEnumerable<PropertyInfo> properties, string? propertyName, bool mustHaveSetter)
    {
        return string.IsNullOrEmpty(propertyName) ?
            default :
            properties.FirstOrDefault(p => p.VerifyGetterSetter(mustHaveSetter) && string.Equals(propertyName, p.Name));
    }

    public static bool VerifyGetterSetter(this PropertyInfo prop, bool mustHaveSetter)
    {
        return prop.GetMethod != default && (!mustHaveSetter || prop.SetMethod != default);
    }

    public static Type? GetListItemType(this Type type)
    {
        var listType = type.GetListType();
        if (listType != default)
        {
            var result = listType.GenericTypeArguments[0];
            return result.IsEntityType() ? result : default;
        }

        return default;
    }

    public static bool IsConstructable(this Type type) => type.IsClass && !type.IsAbstract;

    public static bool IsList(this Type listType) => listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(List<>);

    internal static MapperMetaDataSet? BuildMapperMetaDataSet(Delegate? customPropertiesMapper, MethodMetaData? keyMapper, MethodMetaData? contentMapper)
    {
        return customPropertiesMapper == null && !keyMapper.HasValue && !contentMapper.HasValue
            ? null
            : new MapperMetaDataSet(customPropertiesMapper, keyMapper, contentMapper);
    }

    internal static void AddIfNotExists<T>(this Dictionary<Type, Dictionary<Type, T>> dict, Type sourceType, Type targetType, T value, bool? extraCondition = null)
    {
        if (!extraCondition.HasValue || extraCondition.Value)
        {
            if (!dict.TryGetValue(sourceType, out var innerDict))
            {
                innerDict = new Dictionary<Type, T>();
                dict[sourceType] = innerDict;
            }

            if (!innerDict.ContainsKey(targetType))
            {
                innerDict.Add(targetType, value);
            }
        }
    }

    internal static void AddOrUpdateNull<T>(this Dictionary<Type, Dictionary<Type, T>> dict, Type sourceType, Type targetType, T value, bool? extraCondition = null)
    {
        if (!extraCondition.HasValue || extraCondition.Value)
        {
            if (!dict.TryGetValue(sourceType, out var innerDict))
            {
                innerDict = new Dictionary<Type, T>();
                dict[sourceType] = innerDict;
            }

            if (!innerDict.TryGetValue(targetType, out var existing) || existing == null)
            {
                innerDict[targetType] = value;
            }
        }
    }

    internal static void AddIfNotExists<T>(this Dictionary<Type, Dictionary<Type, T>> dict, Type sourceType, Type targetType, Func<T> func, bool? extraCondition = null)
    {
        if (!extraCondition.HasValue || extraCondition.Value)
        {
            if (!dict.TryGetValue(sourceType, out var innerDict))
            {
                innerDict = new Dictionary<Type, T>();
                dict[sourceType] = innerDict;
            }

            if (!innerDict.ContainsKey(targetType))
            {
                innerDict![targetType] = func();
            }
        }
    }

    internal static T? Pop<T>(this Dictionary<Type, Dictionary<Type, T>> dict, Type sourceType, Type targetType)
    {
        if (dict.TryGetValue(sourceType, out var innerDict) && innerDict.TryGetValue(targetType, out var item))
        {
            innerDict.Remove(targetType);
            if (!innerDict.Any())
            {
                dict.Remove(sourceType);
            }

            return item;
        }

        return default;
    }

    internal static T? Find<T>(this Dictionary<Type, Dictionary<Type, T>> dict, Type sourceType, Type targetType)
    {
        return dict.TryGetValue(sourceType, out var innerDict) && innerDict.TryGetValue(targetType, out var item)
            ? item : default;
    }

    internal static T? Find<T>(this IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, T>> dict, Type sourceType, Type targetType)
    {
        return dict.TryGetValue(sourceType, out var innerDict) && innerDict.TryGetValue(targetType, out var item)
            ? item : default;
    }

    internal static Dictionary<Type, IReadOnlyDictionary<Type, Delegate>> MakeDelegateDictionary(IDictionary<Type, Dictionary<Type, MethodMetaData>> metaDataDict, Type type)
    {
        var result = new Dictionary<Type, IReadOnlyDictionary<Type, Delegate>>();
        foreach (var pair in metaDataDict)
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

public record struct MethodMetaData(Type type, string name);

internal record struct MapperSet(Delegate? customPropertiesMapper, Delegate? keyMapper, Delegate? contentMapper);

internal record struct ExistingTargetTrackerSet(Delegate buildExistingTargetTracker, Delegate startTrackingTarget);

// get method is only needed for id, not for concurrency token, so it's nullable here
internal record struct TypeKeyProxyMetaDataSet(MethodMetaData isEmpty, PropertyInfo property);

// get method is only needed for id, not for concurrency token, so it's nullable here
internal record struct TypeKeyProxy(Delegate isEmpty, PropertyInfo property);

internal record struct MapperMetaDataSet(Delegate? customPropertiesMapper, MethodMetaData? keyMapper, MethodMetaData? contentMapper);

internal record struct ExistingTargetTrackerMetaDataSet(MethodMetaData buildExistingTargetTracker, MethodMetaData startTrackingTarget);