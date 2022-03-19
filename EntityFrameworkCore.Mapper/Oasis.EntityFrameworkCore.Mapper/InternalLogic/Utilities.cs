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
    private static readonly IReadOnlySet<Type> IdTypeSet = new HashSet<Type>
    {
        typeof(int), typeof(long), typeof(uint), typeof(ulong), typeof(string), typeof(short), typeof(ushort), typeof(byte),
    };

    private static readonly IReadOnlySet<Type> TimestampTypeSet = new HashSet<Type>
    {
        typeof(byte[]), typeof(DateTime), typeof(string), typeof(int), typeof(long), typeof(uint), typeof(ulong),
        typeof(short), typeof(ushort), typeof(byte),
    };

    public delegate void MapScalarProperties<TSource, TTarget>(TSource source, TTarget target, IScalarTypeConverter converter)
        where TSource : class
        where TTarget : class;

    public delegate void MapEntityProperties<TSource, TTarget>(TSource source, TTarget target, IEntityPropertyMapper mapper)
        where TSource : class
        where TTarget : class;

    public delegate void MapListProperties<TSource, TTarget>(TSource source, TTarget target, IListPropertyMapper mapper)
        where TSource : class
        where TTarget : class;

    public static IReadOnlySet<Type> IdTypes => IdTypeSet;

    public static IReadOnlySet<Type> TimestampTypes => TimestampTypeSet;

    public static string BuildMethodName(char type, Type sourceType, Type targetType)
    {
        return $"_{type}_{sourceType.FullName!.Replace(".", "_")}__To__{targetType.FullName!.Replace(".", "_")}";
    }

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

    public static string GetIdPropertyname(this TypeConfiguration configuration)
    {
        return configuration.identityColumnName ?? DefaultIdPropertyName;
    }

    public static string GetTimestampPropertyName(this TypeConfiguration configuration)
    {
        return configuration.timestampColumnName ?? DefaultTimestampPropertyName;
    }

    public static Expression<Func<TEntity, bool>> BuildIdEqualsExpression<TEntity>(IIdPropertyNameTracker identityPropertyNameTracker, object? value)
        where TEntity : class
    {
        var type = typeof(TEntity);
        var parameter = Expression.Parameter(typeof(TEntity), "entity");
        var equal = Expression.Equal(
            Expression.Property(parameter, type.GetProperty(identityPropertyNameTracker.GetIdPropertyName<TEntity>(), PublicInstance)!),
            Expression.Constant(value));
        return Expression.Lambda<Func<TEntity, bool>>(equal, parameter);
    }
}

internal record struct MapperSet(Delegate scalarPropertiesMapper, Delegate entityPropertiesMapper, Delegate listPropertiesMapper);