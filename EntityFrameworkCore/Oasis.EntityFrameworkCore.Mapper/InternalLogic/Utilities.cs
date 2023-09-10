namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

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
        IRecursiveMappingContext context)
        where TSource : class
        where TTarget : class
        where TKeyType : struct;

    public delegate bool ScalarPropertyIsEmpty<TEntity>(TEntity entity)
        where TEntity : class;

    public delegate bool ScalarPropertiesAreEqual<TSource, TTarget>(TSource source, TTarget target, IScalarTypeConverter converter)
        where TSource : class
        where TTarget : class;

    public delegate Expression<Func<TTarget, bool>> GetSourceIdListContainsTargetId<TSource, TTarget>(List<TSource> sourceList, IScalarTypeConverter converter)
        where TSource : class
        where TTarget : class;

    public delegate Expression<Func<TTarget, bool>> GetSourceIdEqualsTargetId<TSource, TTarget>(TSource source, IScalarTypeConverter converter)
        where TSource : class
        where TTarget : class;

    public delegate TTarget? EntityTrackerFindById<TSource, TTarget, TKeyType>(Dictionary<TKeyType, object> dict, TSource source, IScalarTypeConverter converter)
        where TSource : class
        where TTarget : class
        where TKeyType : notnull;

    public delegate void EntityTrackerTrackById<TSource, TTarget, TKeyType>(Dictionary<TKeyType, object> dict, TSource source, TTarget target, IScalarTypeConverter converter)
        where TSource : class
        where TTarget : class
        where TKeyType : notnull;

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

    public static Type GetUnderlyingType(this Type type) => Nullable.GetUnderlyingType(type) ?? type;

    internal static MapperMetaDataSet? BuildMapperMetaDataSet(Delegate? customPropertiesMapper, MethodMetaData? keyMapper, MethodMetaData? contentMapper)
    {
        return customPropertiesMapper == null && !keyMapper.HasValue && !contentMapper.HasValue
            ? null
            : new MapperMetaDataSet(customPropertiesMapper, keyMapper, contentMapper);
    }

    internal static bool AddIfNotExists<TKey1, TKey2, TValue>(this Dictionary<TKey1, Dictionary<TKey2, TValue>> dict, TKey1 key1, TKey2 key2, TValue value, bool? extraCondition = null)
        where TKey1 : notnull
        where TKey2 : notnull
    {
        if (!extraCondition.HasValue || extraCondition.Value)
        {
            if (!dict.TryGetValue(key1, out var innerDict))
            {
                innerDict = new Dictionary<TKey2, TValue>();
                dict[key1] = innerDict;
            }

            if (!innerDict.ContainsKey(key2))
            {
                innerDict.Add(key2, value);
                return true;
            }
        }

        return false;
    }

    internal static bool AddOrUpdateNull<TKey1, TKey2, TValue>(this Dictionary<TKey1, Dictionary<TKey2, TValue>> dict, TKey1 key1, TKey2 key2, TValue value, bool? extraCondition = null)
        where TKey1 : notnull
        where TKey2 : notnull
    {
        if (!extraCondition.HasValue || extraCondition.Value)
        {
            if (!dict.TryGetValue(key1, out var innerDict))
            {
                innerDict = new Dictionary<TKey2, TValue>();
                dict[key1] = innerDict;
            }

            if (!innerDict.TryGetValue(key2, out var existing) || existing == null)
            {
                innerDict[key2] = value;
                return true;
            }
        }

        return false;
    }

    internal static bool AddIfNotExists<TKey1, TKey2, TValue>(this Dictionary<TKey1, Dictionary<TKey2, TValue>> dict, TKey1 key1, TKey2 key2, Func<TValue> func, bool? extraCondition = null)
        where TKey1 : notnull
        where TKey2 : notnull
    {
        if (!extraCondition.HasValue || extraCondition.Value)
        {
            if (!dict.TryGetValue(key1, out var innerDict))
            {
                innerDict = new Dictionary<TKey2, TValue>();
                dict[key1] = innerDict;
            }

            if (!innerDict.ContainsKey(key2))
            {
                innerDict![key2] = func();
                return true;
            }
        }

        return false;
    }

    internal static TValue? Pop<TKey1, TKey2, TValue>(this Dictionary<TKey1, Dictionary<TKey2, TValue>> dict, TKey1 key1, TKey2 key2)
        where TKey1 : notnull
        where TKey2 : notnull
    {
        if (dict.TryGetValue(key1, out var innerDict) && innerDict.Remove(key2, out var item))
        {
            if (!innerDict.Any())
            {
                dict.Remove(key1);
            }

            return item;
        }

        return default;
    }

    internal static TValue? Find<TKey1, TKey2, TValue>(this Dictionary<TKey1, Dictionary<TKey2, TValue>> dict, TKey1 key1, TKey2 key2)
        where TKey1 : notnull
        where TKey2 : notnull
    {
        return dict.TryGetValue(key1, out var innerDict) && innerDict.TryGetValue(key2, out var item)
            ? item : default;
    }

    internal static bool Contains<TKey1, TKey2, TValue>(this Dictionary<TKey1, Dictionary<TKey2, TValue>> dict, TKey1 key1, TKey2 key2)
        where TKey1 : notnull
        where TKey2 : notnull
    {
        return dict.TryGetValue(key1, out var innerDict) && innerDict.ContainsKey(key2);
    }

    internal static TValue? Find<TKey1, TKey2, TValue>(this IReadOnlyDictionary<TKey1, IReadOnlyDictionary<TKey2, TValue>> dict, TKey1 key1, TKey2 key2)
        where TKey1 : notnull
        where TKey2 : notnull
    {
        return dict.TryGetValue(key1, out var innerDict) && innerDict.TryGetValue(key2, out var item)
            ? item : default;
    }

    internal static Dictionary<TKey1, IReadOnlyDictionary<TKey2, Delegate>> MakeDelegateDictionary<TKey1, TKey2>(IReadOnlyDictionary<TKey1, Dictionary<TKey2, MethodMetaData>> metaDataDict, Type type)
        where TKey1 : notnull
        where TKey2 : notnull
    {
        var result = new Dictionary<TKey1, IReadOnlyDictionary<TKey2, Delegate>>();
        foreach (var pair in metaDataDict)
        {
            var innerDictionary = new Dictionary<TKey2, Delegate>();
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

internal record struct MethodMetaData(Type type, string name);

internal record struct TargetByIdTrackerMethods(Delegate find, Delegate track);

internal record struct MapperSet(Delegate? customPropertiesMapper, Delegate? keyMapper, Delegate? contentMapper);

// get method is only needed for id, not for concurrency token, so it's nullable here
internal record struct TypeKeyProxyMetaDataSet(MethodMetaData isEmpty, PropertyInfo property);

// get method is only needed for id, not for concurrency token, so it's nullable here
internal record struct TypeKeyProxy(Delegate isEmpty, PropertyInfo property);

internal record struct MapperMetaDataSet(Delegate? customPropertiesMapper, MethodMetaData? keyMapper, MethodMetaData? contentMapper);

internal record struct TargetByIdTrackerMetaDataSet(MethodMetaData find, MethodMetaData track);