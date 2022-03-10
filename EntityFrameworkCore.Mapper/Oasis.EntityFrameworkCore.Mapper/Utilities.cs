namespace Oasis.EntityFrameworkCore.Mapper;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Reflection;

internal static class Utilities
{
    private static readonly Type EntityBaseType = typeof(EntityBase);

    public delegate void MapScalarProperties<TSource, TTarget>(TSource source, TTarget target)
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase;

    public delegate void MapListProperties<TSource, TTarget>(TSource source, TTarget target, IListPropertyMapper mapper)
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase;

    public static bool IsScalarType(this PropertyInfo prop, bool mustHaveGetter, bool mustHaveSetter)
    {
        const string NullableTypeName = "System.Nullable`1[[";
        var type = prop.PropertyType;
        var name = prop.Name;
        return ((type.IsValueType && (type.IsPrimitive || (type.FullName!.StartsWith(NullableTypeName) && type.GenericTypeArguments.Length == 1) && type.GenericTypeArguments[0].IsPrimitive)) || type == typeof(string) || type == typeof(byte[]))
            && (!mustHaveGetter || prop.GetMethod != null) && (!mustHaveSetter || prop.SetMethod != null)
            && !string.Equals(name, nameof(EntityBase.Id)) && !string.Equals(name, nameof(EntityBase.Timestamp));
    }

    public static bool IsListNavigationType(this PropertyInfo prop, bool mustHaveGetter, bool mustHaveSetter)
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

internal record struct MapperSet(Delegate ScalarPropertiesMapper, Delegate ListPropertiesMapper);