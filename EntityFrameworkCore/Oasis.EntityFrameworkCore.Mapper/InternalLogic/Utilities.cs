namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

internal static class Utilities
{
    public const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

    public delegate void MapScalarProperties<TSource, TTarget>(TSource source, TTarget target, IScalarTypeConverter converter)
        where TSource : class
        where TTarget : class;

    public delegate void MapProperties<TSource, TTarget, TKeyType>(
        TSource source,
        TTarget target,
        IScalarTypeConverter converter,
        IRecursiveMapper<TKeyType> mapper,
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

    internal static MapperMetaDataSet? BuildMapperMetaDataSet(Delegate? customPropertiesMapper, MethodMetaData? keyMapper, MethodMetaData? contentMapper)
    {
        return customPropertiesMapper == null && !keyMapper.HasValue && !contentMapper.HasValue
            ? null
            : new MapperMetaDataSet(customPropertiesMapper, keyMapper, contentMapper);
    }

    internal static void Add<T>(this Dictionary<Type, Dictionary<Type, T>> dict, Type sourceType, Type targetType, T value, bool? extraCondition = null)
    {
        if (!dict.TryGetValue(sourceType, out var innerDict))
        {
            innerDict = new Dictionary<Type, T>();
            dict[sourceType] = innerDict;
        }

        if (!innerDict.ContainsKey(targetType) && (!extraCondition.HasValue || extraCondition.Value))
        {
            innerDict![targetType] = value;
        }
    }

    internal static void Add<T>(this Dictionary<Type, Dictionary<Type, T>> dict, Type sourceType, Type targetType, Func<T> func, bool? extraCondition = null)
    {
        if (!dict.TryGetValue(sourceType, out var innerDict))
        {
            innerDict = new Dictionary<Type, T>();
            dict[sourceType] = innerDict;
        }

        if (!innerDict.ContainsKey(targetType) && (!extraCondition.HasValue || extraCondition.Value))
        {
            innerDict![targetType] = func();
        }
    }

    internal static T? Pop<T>(this Dictionary<Type, Dictionary<Type, T>> dict, Type sourceType, Type targetType)
    {
        if (dict.TryGetValue(sourceType, out var innerDict) && innerDict.Remove(targetType, out var item))
        {
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
}

internal record struct MethodMetaData(Type type, string name);

internal record struct MapperSet(Delegate? customPropertiesMapper, Delegate? keyMapper, Delegate? contentMapper);

// get method is only needed for id, not for concurrency token, so it's nullable here
internal record struct TypeKeyProxyMetaDataSet(MethodMetaData? get, MethodMetaData isEmpty, PropertyInfo property);

// get method is only needed for id, not for concurrency token, so it's nullable here
internal record struct TypeKeyProxy(Delegate? get, Delegate isEmpty, PropertyInfo property);

internal record struct MapperMetaDataSet(Delegate? customPropertiesMapper, MethodMetaData? keyMapper, MethodMetaData? contentMapper);