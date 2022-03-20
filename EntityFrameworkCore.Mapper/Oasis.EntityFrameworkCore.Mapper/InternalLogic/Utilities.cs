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
    private static readonly Type EnumerableType = typeof(IEnumerable);

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

    public static bool IsScalarType(this Type type)
    {
        const string NullableTypeName = "System.Nullable`1[[";
        return (type.IsValueType && (type.IsPrimitive || ((type.FullName!.StartsWith(NullableTypeName) && type.GenericTypeArguments.Length == 1) && type.GenericTypeArguments[0].IsPrimitive))) || type == typeof(string) || type == typeof(byte[]);
    }

    public static bool IsScalarProperty(this PropertyInfo prop, IReadOnlySet<Type> convertables, bool mustHaveGetter, bool mustHaveSetter)
    {
        var type = prop.PropertyType;
        return (IsScalarType(type) || convertables.Contains(type))
            && (!mustHaveGetter || prop.GetMethod != default) && (!mustHaveSetter || prop.SetMethod != default);
    }

    public static bool IsEntityType(this Type type)
    {
        return (type.IsClass || type.IsInterface) && !type.GetInterfaces().Contains(EnumerableType);
    }

    public static bool IsEntityProperty(this PropertyInfo prop, bool mustHaveGetter, bool mustHaveSetter)
    {
        return IsEntityType(prop.PropertyType)
            && (!mustHaveGetter || prop.GetMethod != default) && (!mustHaveSetter || prop.SetMethod != default);
    }

    public static bool IsListOfEntityType(this Type type)
    {
        const string ICollectionTypeName = "System.Collections.Generic.ICollection`1[[";
        const string IListTypeName = "System.Collections.Generic.IList`1[[";
        const string ListTypeName = "System.Collections.Generic.List`1[[";
        var typeFullName = type.FullName;
        return (typeFullName!.StartsWith(ICollectionTypeName) || typeFullName.StartsWith(IListTypeName) || typeFullName.StartsWith(ListTypeName))
            && type.GenericTypeArguments.Length == 1 && type.GenericTypeArguments[0].IsEntityType();
    }

    public static bool IsListOfEntityProperty(this PropertyInfo prop, bool mustHaveGetter, bool mustHaveSetter)
    {
        return IsListOfEntityType(prop.PropertyType)
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
        var type = typeof(TEntity);
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