﻿namespace Oasis.EntityFramework.Mapper.InternalLogic;

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
        return (type.IsValueType && (type.IsPrimitive || type.IsEnum))
            || (type.IsNullable(out var argumentType) && (argumentType!.IsPrimitive || argumentType.IsEnum))
            || NonPrimitiveScalarTypes.Contains(type);
    }

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

    public static bool IsNullable(this Type type, out Type? argumentType)
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
    private readonly ISet<Type> _convertableToScalarTypes;

    public ScalarMapperTypeValidator(
        IReadOnlyDictionary<Type, Dictionary<Type, Delegate>> scalarConverterDictionary,
        ISet<Type> convertableToScalarTypes)
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