namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using System.Collections;

internal interface IMapperTypeValidator
{
    bool IsSourceType(Type type);

    bool IsTargetType(Type type);

    bool CanConvert(Type sourceType, Type targetType);
}

internal static class MapperTypeValidatorExtensions
{
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

    public static bool IsScalarType(this Type type)
    {
        return (type.IsValueType && (type.IsPrimitive || type.IsNullablePrimitive())) || NonPrimitiveScalarTypes.Contains(type);
    }

    public static bool IsEntityType(this Type type)
    {
        return type.IsClass && !NonEntityClassTypes.Contains(type) && !IsOfGenericTypeDefinition(type, EnumerableType) && !type.GetInterfaces().Any(i => IsOfGenericTypeDefinition(i, EnumerableType));
    }

    public static Type? GetListType(this Type type)
    {
        return type.IsArray ? default :
            IsOfGenericTypeDefinition(type, CollectionType) ?
                type : type.GetInterfaces().FirstOrDefault(i => IsOfGenericTypeDefinition(i, CollectionType));
    }

    public static bool IsNullablePrimitive(this Type type)
    {
        const string NullableTypeName = "System.Nullable`1[[";
        return type.FullName!.StartsWith(NullableTypeName) && type.GenericTypeArguments.Length == 1 && type.GenericTypeArguments[0].IsPrimitive;
    }

    private static bool IsOfGenericTypeDefinition(Type source, Type target)
    {
        return source.IsGenericType && source.GetGenericTypeDefinition() == target;
    }
}

internal sealed class ScalarMapperTypeValidator : IMapperTypeValidator
{
    private readonly IReadOnlyDictionary<Type, Dictionary<Type, Delegate>> _scalarConverterDictionary;
    private readonly IReadOnlySet<Type> _convertableToScalarTargetTypes;

    public ScalarMapperTypeValidator(
        IReadOnlyDictionary<Type, Dictionary<Type, Delegate>> scalarConverterDictionary,
        IReadOnlySet<Type> convertableToScalarTargetTypes)
    {
        _scalarConverterDictionary = scalarConverterDictionary;
        _convertableToScalarTargetTypes = convertableToScalarTargetTypes;
    }

    public bool CanConvert(Type sourceType, Type targetType)
        => _scalarConverterDictionary.TryGetValue(sourceType, out var inner) && inner.ContainsKey(targetType);

    public bool IsSourceType(Type type) => type.IsScalarType() || _scalarConverterDictionary.ContainsKey(type);

    public bool IsTargetType(Type type) => type.IsScalarType() || _convertableToScalarTargetTypes.Contains(type);
}

internal sealed class EntityMapperTypeValidator : IMapperTypeValidator
{
    private readonly IReadOnlyDictionary<Type, Dictionary<Type, MapperMetaDataSet>> _mapperDictionary;
    private readonly IReadOnlySet<Type> _convertableToScalarSourceTypes;
    private readonly IReadOnlySet<Type> _convertableToScalarTargetTypes;

    public EntityMapperTypeValidator(
        IReadOnlyDictionary<Type, Dictionary<Type, MapperMetaDataSet>> mapperDictionary,
        IReadOnlySet<Type> convertableToScalarSourceTypes,
        IReadOnlySet<Type> convertableToScalarTargetTypes)
    {
        _mapperDictionary = mapperDictionary;
        _convertableToScalarSourceTypes = convertableToScalarSourceTypes;
        _convertableToScalarTargetTypes = convertableToScalarTargetTypes;
    }

    public bool CanConvert(Type sourceType, Type targetType)
        => _mapperDictionary.TryGetValue(sourceType, out var inner) && inner.ContainsKey(targetType);

    public bool IsSourceType(Type type) => type.IsEntityType() && !_convertableToScalarSourceTypes.Contains(type);

    public bool IsTargetType(Type type) => type.IsEntityType() && !_convertableToScalarTargetTypes.Contains(type);
}

internal sealed class EntityListMapperTypeValidator : IMapperTypeValidator
{
    private readonly IReadOnlyDictionary<Type, Dictionary<Type, MapperMetaDataSet>> _mapperDictionary;
    private readonly IReadOnlySet<Type> _convertableToScalarSourceTypes;
    private readonly IReadOnlySet<Type> _convertableToScalarTargetTypes;

    public EntityListMapperTypeValidator(
        IReadOnlyDictionary<Type, Dictionary<Type, MapperMetaDataSet>> mapperDictionary,
        IReadOnlySet<Type> convertableToScalarSourceTypes,
        IReadOnlySet<Type> convertableToScalarTargetTypes)
    {
        _mapperDictionary = mapperDictionary;
        _convertableToScalarSourceTypes = convertableToScalarSourceTypes;
        _convertableToScalarTargetTypes = convertableToScalarTargetTypes;
    }

    /// <summary>
    /// Note that we use item type intead of list type here for performance considerations.
    /// </summary>
    /// <param name="sourceItemType">Type of source list item.</param>
    /// <param name="targetItemType">Type of target list item.</param>
    /// <returns>True if conversion can happen, else false.</returns>
    public bool CanConvert(Type sourceItemType, Type targetItemType)
        => _mapperDictionary.TryGetValue(sourceItemType, out var inner) && inner.ContainsKey(targetItemType);

    public bool IsSourceType(Type type)
    {
        return TryGetListItemType(type, out var itemType) && !_convertableToScalarSourceTypes.Contains(itemType!);
    }

    public bool IsTargetType(Type type)
    {
        return TryGetListItemType(type, out var itemType) && !_convertableToScalarTargetTypes.Contains(itemType!);
    }

    private static bool TryGetListItemType(Type type, out Type? itemType)
    {
        itemType = type.GetListItemType();
        return itemType != default;
    }
}