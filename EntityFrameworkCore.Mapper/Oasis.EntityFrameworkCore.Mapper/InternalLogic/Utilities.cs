namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using System.Reflection;

internal static class Utilities
{
    internal const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
    internal const BindingFlags NonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;
    private static readonly Type EntityBaseType = typeof(EntityBase);

    public delegate void MapScalarProperties<TSource, TTarget>(TSource source, TTarget target)
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase;

    public delegate void MapListProperties<TSource, TTarget>(TSource source, TTarget target, IListPropertyMapper mapper)
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase;

    public static bool IsScalarType(this Type type)
    {
        const string NullableTypeName = "System.Nullable`1[[";
        return (type.IsValueType && (type.IsPrimitive || ((type.FullName!.StartsWith(NullableTypeName) && type.GenericTypeArguments.Length == 1) && type.GenericTypeArguments[0].IsPrimitive))) || type == typeof(string) || type == typeof(byte[]);
    }

    public static bool IsScalarProperty(this PropertyInfo prop, ISet<Type> convertables, bool mustHaveGetter, bool mustHaveSetter)
    {
        var name = prop.Name;
        var type = prop.PropertyType;
        return (IsScalarType(type) || convertables.Contains(type))
            && (!mustHaveGetter || prop.GetMethod != null) && (!mustHaveSetter || prop.SetMethod != null)
            && !string.Equals(name, nameof(EntityBase.Id)) && !string.Equals(name, nameof(EntityBase.Timestamp));
    }

    public static bool IsListOfNavigationProperty(this PropertyInfo prop, bool mustHaveGetter, bool mustHaveSetter)
    {
        const string ICollectionTypeName = "System.Collections.Generic.ICollection`1[[";
        const string IListTypeName = "System.Collections.Generic.IList`1[[";
        const string ListTypeName = "System.Collections.Generic.List`1[[";
        var type = prop.PropertyType;
        var typeFullName = type.FullName;
        return (typeFullName!.StartsWith(ICollectionTypeName) || typeFullName.StartsWith(IListTypeName) || typeFullName.StartsWith(ListTypeName))
            && type.GenericTypeArguments.Length == 1 && type.GenericTypeArguments[0].IsSubclassOf(EntityBaseType)
            && (!mustHaveGetter || prop.GetMethod != null) && (!mustHaveSetter || prop.SetMethod != null);
    }
}

internal record struct MapperSet(Delegate scalarPropertiesMapper, Delegate listPropertiesMapper);