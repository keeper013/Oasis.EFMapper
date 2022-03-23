namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

internal static class Utilities
{
    public const string DefaultIdPropertyName = "Id";
    public const string DefaultTimestampPropertyName = "Timestamp";
    public const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
    public const BindingFlags NonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;
    public static readonly Type StringType = typeof(string);
    public static readonly Type ByteArrayType = typeof(byte[]);
    public static readonly Type DecimalType = typeof(decimal);
    public static readonly Type NullableDecimalType = typeof(decimal?);
    public static readonly Type DateTimeType = typeof(DateTime);
    public static readonly Type NullableDateTimeType = typeof(DateTime?);
    private static readonly Type EnumerableType = typeof(IEnumerable);
    private static readonly Type[] NonPrimitiveScalarTypes = new[] { StringType, ByteArrayType, DecimalType, NullableDecimalType, DateTimeType, NullableDateTimeType };

    public delegate void MapScalarProperties<TSource, TTarget>(TSource source, TTarget target, IScalarTypeConverter converter)
        where TSource : class
        where TTarget : class;

    public delegate void MapEntityProperties<TSource, TTarget>(TSource source, TTarget target, IEntityPropertyMapper mapper)
        where TSource : class
        where TTarget : class;

    public delegate void MapListProperties<TSource, TTarget>(TSource source, TTarget target, IListPropertyMapper mapper)
        where TSource : class
        where TTarget : class;

    public delegate object GetId<TEntity>(TEntity entity)
        where TEntity : class;

    public delegate bool IdIsEmpty<TEntity>(TEntity entity)
        where TEntity : class;

    public delegate bool TimeStampIsEmpty<TEntity>(TEntity entity)
        where TEntity : class;

    public delegate bool IdsAreEqual<TSource, TTarget>(TSource source, TTarget target, IScalarTypeConverter converter)
        where TSource : class
        where TTarget : class;

    public delegate bool TimeStampsAreEqual<TSource, TTarget>(TSource source, TTarget target, IScalarTypeConverter converter)
        where TSource : class
        where TTarget : class;

    public static bool IsNullablePrimitive(this Type type)
    {
        const string NullableTypeName = "System.Nullable`1[[";
        return type.FullName!.StartsWith(NullableTypeName) && type.GenericTypeArguments.Length == 1 && type.GenericTypeArguments[0].IsPrimitive;
    }

    public static bool IsScalarType(this Type type)
    {
        return (type.IsValueType && (type.IsPrimitive || type.IsNullablePrimitive())) || NonPrimitiveScalarTypes.Contains(type);
    }

    public static bool IsScalarProperty(this PropertyInfo prop, IReadOnlySet<Type> convertables, bool mustHaveGetter, bool mustHaveSetter)
    {
        var type = prop.PropertyType;
        return (type.IsScalarType() || convertables.Contains(type))
            && (!mustHaveGetter || prop.GetMethod != default) && (!mustHaveSetter || prop.SetMethod != default);
    }

    public static bool IsEntityType(this Type type, IReadOnlySet<Type> convertables)
    {
        return type.IsClass && !convertables.Contains(type) && !type.GetInterfaces().Contains(EnumerableType);
    }

    public static bool IsEntityProperty(this PropertyInfo prop, IReadOnlySet<Type> convertables, bool mustHaveGetter, bool mustHaveSetter)
    {
        return prop.PropertyType.IsEntityType(convertables)
            && (!mustHaveGetter || prop.GetMethod != default) && (!mustHaveSetter || prop.SetMethod != default);
    }

    public static bool IsListOfEntityType(this Type type, IReadOnlySet<Type> convertables)
    {
        const string ICollectionTypeName = "System.Collections.Generic.ICollection`1[[";
        const string IListTypeName = "System.Collections.Generic.IList`1[[";
        const string ListTypeName = "System.Collections.Generic.List`1[[";
        var typeFullName = type.FullName;
        return (typeFullName!.StartsWith(ICollectionTypeName) || typeFullName.StartsWith(IListTypeName) || typeFullName.StartsWith(ListTypeName))
            && type.GenericTypeArguments.Length == 1 && type.GenericTypeArguments[0].IsEntityType(convertables);
    }

    public static bool IsListOfEntityProperty(this PropertyInfo prop, IReadOnlySet<Type> convertables, bool mustHaveGetter, bool mustHaveSetter)
    {
        return prop.PropertyType.IsListOfEntityType(convertables)
            && (!mustHaveGetter || prop.GetMethod != default) && (!mustHaveSetter || prop.SetMethod != default);
    }

    public static bool ItemExists<T>(this Dictionary<Type, Dictionary<Type, T>> dict, Type sourceType, Type targetType)
    {
        return dict.TryGetValue(sourceType, out var innerDictionary) && innerDictionary.ContainsKey(targetType);
    }

    public static PropertyInfo GetIdProperty(this TypeProxy proxy)
    {
        return proxy.identityProperty;
    }

    public static string GetIdPropertyName(this TypeConfiguration configuration)
    {
        return configuration.identityPropertyName ?? DefaultIdPropertyName;
    }

    public static string GetTimestampPropertyName(this TypeConfiguration configuration)
    {
        return configuration.timestampPropertyName ?? DefaultTimestampPropertyName;
    }

    public static Expression<Func<TEntity, bool>> BuildIdEqualsExpression<TEntity>(IIdPropertyTracker identityPropertyTracker, object? value)
        where TEntity : class
    {
        var parameter = Expression.Parameter(typeof(TEntity), "entity");
        var equal = Expression.Equal(
            Expression.Property(parameter, identityPropertyTracker.GetIdProperty<TEntity>()),
            Expression.Constant(value));
        return Expression.Lambda<Func<TEntity, bool>>(equal, parameter);
    }
}

internal record struct MethodMetaData(Type type, string name);

internal record struct MapperSet(Delegate scalarPropertiesMapper, Delegate entityPropertiesMapper, Delegate listPropertiesMapper);

internal record struct TypeProxyMetaDataSet(MethodMetaData getId, MethodMetaData identityIsEmpty, MethodMetaData timestampIsEmpty, PropertyInfo identityProperty, bool keepEntityOnMappingRemoved);

internal record struct TypeProxy(Delegate getId, Delegate identityIsEmpty, Delegate timestampIsEmpty, PropertyInfo identityProperty, bool keepEntityOnMappingRemoved);

internal record struct EntityComparer(Delegate idsAreEqual, Delegate timestampsAreEqual);