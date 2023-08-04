namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

internal interface IMapperTypeValidator
{
    bool IsValidType(Type type);

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

    public static bool IsListOfEntityType(this Type type)
    {
        var itemType = type.GetListItemType();
        return itemType != null ? itemType.IsEntityType() : false;
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

internal abstract class MapperTypeValidator<T> : IMapperTypeValidator
{
    private readonly IReadOnlyDictionary<Type, Dictionary<Type, T>> _mapperDictionary;

    public MapperTypeValidator(IReadOnlyDictionary<Type, Dictionary<Type, T>> mapperDictionary)
    {
        _mapperDictionary = mapperDictionary;
    }

    public bool CanConvert(Type sourceType, Type targetType)
        => _mapperDictionary.TryGetValue(sourceType, out var inner) && inner.ContainsKey(targetType);

    public abstract bool IsValidType(Type type);
}

internal sealed class ScalarMapperTypeValidator : MapperTypeValidator<Delegate>
{
    private readonly IReadOnlySet<Type> _convertableToScalarTypes;

    public ScalarMapperTypeValidator(
        IReadOnlyDictionary<Type, Dictionary<Type, Delegate>> scalarConverterDictionary,
        IReadOnlySet<Type> convertableToScalarTypes)
        : base(scalarConverterDictionary)
    {
        _convertableToScalarTypes = convertableToScalarTypes;
    }

    public override bool IsValidType(Type type) => type.IsScalarType() || _convertableToScalarTypes.Contains(type);
}

internal sealed class EntityMapperTypeValidator : MapperTypeValidator<MapperMetaDataSet?>
{
    public EntityMapperTypeValidator(IReadOnlyDictionary<Type, Dictionary<Type, MapperMetaDataSet?>> mapperDictionary)
        : base(mapperDictionary)
    {
    }

    public override bool IsValidType(Type type) => type.IsEntityType();
}

internal sealed class EntityListMapperTypeValidator : MapperTypeValidator<MapperMetaDataSet?>
{
    public EntityListMapperTypeValidator(IReadOnlyDictionary<Type, Dictionary<Type, MapperMetaDataSet?>> mapperDictionary)
        : base(mapperDictionary)
    {
    }

    public override bool IsValidType(Type type)
    {
        return type.IsListOfEntityType();
    }
}