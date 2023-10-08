namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

internal static class Utilities
{
    public const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
    public const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;

    private static readonly Type EnumerableType = typeof(IEnumerable<>);
    private static readonly Type CollectionType = typeof(ICollection<>);
    private static readonly Type[] NonPrimitiveScalarTypes = new[]
    {
        typeof(string), typeof(byte[]), typeof(decimal), typeof(decimal?), typeof(DateTime), typeof(DateTime?),
    };

    private static readonly Type[] NonEntityClassTypes = new[]
    {
        typeof(string), typeof(byte[]),
    };

    public delegate void MapToMemory<TSource, TTarget, TKeyType>(
        TSource source,
        TTarget target,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> converter,
        IRecursiveMapper<TKeyType> mapper,
        IRecursiveMappingContext context)
        where TSource : class
        where TTarget : class
        where TKeyType : struct;

    public delegate void MapToDatabase<TSource, TTarget, TKeyType>(
        TSource source,
        TTarget target,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> converter,
        IRecursiveMapper<TKeyType> mapper,
        IRecursiveMappingContext context,
        bool mapId)
        where TSource : class
        where TTarget : class
        where TKeyType : struct;

    public delegate bool ScalarPropertyIsEmpty<TEntity>(TEntity entity)
        where TEntity : class;

    public delegate bool ScalarPropertiesAreEqual<TSource, TTarget>(TSource source, TTarget target, IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> converter)
        where TSource : class
        where TTarget : class;

    public delegate Expression<Func<TTarget, bool>> GetSourceIdListContainsTargetId<TSource, TTarget>(List<TSource> sourceList, IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> converter)
        where TSource : class
        where TTarget : class;

    public delegate Expression<Func<TTarget, bool>> GetSourceIdEqualsTargetId<TSource, TTarget>(TSource source, IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> converter)
        where TSource : class
        where TTarget : class;

    public delegate TTarget? EntityTrackerFindById<TSource, TTarget, TKeyType>(Dictionary<TKeyType, object> dict, TSource source, IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> converter)
        where TSource : class
        where TTarget : class
        where TKeyType : notnull;

    public delegate void EntityTrackerTrackById<TSource, TTarget, TKeyType>(Dictionary<TKeyType, object> dict, TSource source, TTarget target, IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> converter)
        where TSource : class
        where TTarget : class
        where TKeyType : notnull;

    public static bool AllowsInsert(this MapType mapType) => (mapType & MapType.Insert) == MapType.Insert;

    public static bool AllowsUpdate(this MapType mapType) => (mapType & MapType.Update) == MapType.Update;

    public static bool AllowsMappingToMemory(this MapType mapType) => (mapType & MapType.Memory) == MapType.Memory;

    public static bool AllowsMappingToDatabase(this MapType mapType) => (mapType & MapType.Upsert) != 0;

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

    public static bool IsScalarType(this Type type) => type.IsValueType || type.IsNullable(out _) || NonPrimitiveScalarTypes.Contains(type);

    public static bool IsEntityType(this Type type)
    {
        return (type.IsClass || type.IsInterface) && !NonEntityClassTypes.Contains(type) && !IsOfGenericTypeDefinition(type, EnumerableType) && !type.GetInterfaces().Any(i => IsOfGenericTypeDefinition(i, EnumerableType));
    }

    public static bool IsListOfEntityType(this Type type)
    {
        var itemType = type.GetListItemType();
        return itemType != null && itemType.IsEntityType();
    }

    public static Type? GetListType(this Type type)
    {
        if (type.IsArray)
        {
            return default;
        }

        if (IsOfGenericTypeDefinition(type, CollectionType))
        {
            return type;
        }

        var types = type.GetInterfaces().Where(i => IsOfGenericTypeDefinition(i, CollectionType)).ToList();
        return types.Count == 1 ? types[0] : default;
    }

    public static bool IsNullable(this Type type, [NotNullWhen(true)] out Type? argumentType)
    {
        const string NullableTypeName = "System.Nullable`1[[";
        if (type.FullName!.StartsWith(NullableTypeName) && type.GenericTypeArguments.Length == 1)
        {
            argumentType = type.GenericTypeArguments[0];
            return true;
        }

        argumentType = null;
        return false;
    }

    internal static void Add<TKey1, TKey2, TValue>(this Dictionary<TKey1, Dictionary<TKey2, TValue>> dict, TKey1 key1, TKey2 key2, TValue value)
        where TKey1 : notnull
        where TKey2 : notnull
    {
        if (!dict.TryGetValue(key1, out var innerDict))
        {
            innerDict = new Dictionary<TKey2, TValue>();
            dict[key1] = innerDict;
        }

        innerDict.Add(key2, value);
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

    internal static bool Add<TKey1, TKey2>(this Dictionary<TKey1, HashSet<TKey2>> dict, TKey1 key1, TKey2 key2)
        where TKey1 : notnull
        where TKey2 : notnull
    {
        if (!dict.TryGetValue(key1, out var hashSet))
        {
            hashSet = new HashSet<TKey2>();
            dict[key1] = hashSet;
        }

        return hashSet.Add(key2);
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
        => dict.TryGetValue(key1, out var innerDict) && innerDict.TryGetValue(key2, out var item) ? item : default;

    internal static bool Contains<TKey1, TKey2, TValue>(this Dictionary<TKey1, Dictionary<TKey2, TValue>> dict, TKey1 key1, TKey2 key2)
        where TKey1 : notnull
        where TKey2 : notnull
        => dict.TryGetValue(key1, out var innerDict) && innerDict.ContainsKey(key2);

    internal static bool Contains<TKey1, TKey2>(this IReadOnlyDictionary<TKey1, IReadOnlySet<TKey2>> dict, TKey1 key1, TKey2 key2)
        where TKey1 : notnull
        where TKey2 : notnull
        => dict.TryGetValue(key1, out var inner) && inner.Contains(key2);

    internal static bool Contains<TKey1, TKey2>(this Dictionary<TKey1, HashSet<TKey2>> dict, TKey1 key1, TKey2 key2)
        where TKey1 : notnull
        where TKey2 : notnull
        => dict.TryGetValue(key1, out var inner) && inner.Contains(key2);

    internal static TValue? Find<TKey1, TKey2, TValue>(this IReadOnlyDictionary<TKey1, IReadOnlyDictionary<TKey2, TValue>> dict, TKey1 key1, TKey2 key2)
        where TKey1 : notnull
        where TKey2 : notnull
        => dict.TryGetValue(key1, out var innerDict) && innerDict.TryGetValue(key2, out var item) ? item : default;

    internal static TValue DefaultIfNotFound<TKey1, TKey2, TValue>(this IReadOnlyDictionary<TKey1, IReadOnlyDictionary<TKey2, TValue>> dict, TKey1 key1, TKey2 key2, TValue defaultValue)
        where TKey1 : notnull
        where TKey2 : notnull
        => dict.TryGetValue(key1, out var innerDict) && innerDict.TryGetValue(key2, out var item) ? item : defaultValue;

    private static bool IsOfGenericTypeDefinition(Type source, Type target) => source.IsGenericType && source.GetGenericTypeDefinition() == target;
}

internal record struct MethodMetaData(Type type, string name);

internal record struct TargetByIdTrackerMethods(Delegate find, Delegate track);

internal record struct TypeKeyProxyMetaDataSet(MethodMetaData isEmpty, PropertyInfo property);

internal record struct TypeKeyProxy(Delegate isEmpty, PropertyInfo property);

internal record struct TargetByIdTrackerMetaDataSet(MethodMetaData find, MethodMetaData track);