namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using System.Reflection;

internal static class Utilities
{
    internal const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
    internal const BindingFlags NonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;
    private static readonly Type EntityBaseType = typeof(EntityBase);

    public delegate void MapScalarProperties<TSource, TTarget>(TSource source, TTarget target, IScalarTypeConverter converter)
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase;

    public delegate void MapListProperties<TSource, TTarget>(TSource source, TTarget target, IListPropertyMapper mapper)
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase;

    public static string BuildMethodName(char type, Type sourceType, Type targetType)
    {
        return $"_{type}_{sourceType.FullName!.Replace(".", "_")}__To__{targetType.FullName!.Replace(".", "_")}";
    }

    public static MethodInfo GetGenericMethod(IDictionary<Type, IDictionary<Type, MethodInfo>> dict, MethodInfo template, Type sourceType, Type targetType)
    {
        if (!dict.TryGetValue(sourceType, out var innerDictionary))
        {
            innerDictionary = new Dictionary<Type, MethodInfo>();
            dict[sourceType] = innerDictionary;
        }

        if (!innerDictionary.TryGetValue(targetType, out var method))
        {
            method = template.MakeGenericMethod(sourceType, targetType);
            innerDictionary[targetType] = method;
        }

        return method;
    }

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
            && (!mustHaveGetter || prop.GetMethod != null) && (!mustHaveSetter || prop.SetMethod != null);
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

    public static bool ItemExists<T>(this IDictionary<Type, IDictionary<Type, T>> dict, Type sourceType, Type targetType)
    {
        return dict.TryGetValue(sourceType, out var innerDictionary) && innerDictionary.ContainsKey(targetType);
    }
}

internal record struct MapperSet(Delegate scalarPropertiesMapper, Delegate listPropertiesMapper);