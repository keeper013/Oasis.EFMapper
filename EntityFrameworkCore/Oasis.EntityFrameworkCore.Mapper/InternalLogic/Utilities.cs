namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using System.Reflection;

internal static class Utilities
{
    public const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

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

    public delegate bool IdsAreEqual<TSource, TTarget>(TSource source, TTarget target, IScalarTypeConverter converter)
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
}

internal record struct MethodMetaData(Type type, string name);

internal record struct MapperSet(Delegate keyPropertiesMapper, Delegate scalarPropertiesMapper, Delegate entityPropertiesMapper, Delegate listPropertiesMapper);

internal record struct TypeProxyMetaDataSet(MethodMetaData getId, MethodMetaData identityIsEmpty, PropertyInfo identityProperty, bool keepEntityOnMappingRemoved);

internal record struct TypeProxy(Delegate getId, Delegate identityIsEmpty, PropertyInfo identityProperty);

internal record struct EntityComparer(Delegate idsAreEqual);

internal record struct MapperMetaDataSet(MethodMetaData keyPropertiesMapper, MethodMetaData scalarPropertiesMapper, MethodMetaData entityPropertiesMapper, MethodMetaData listPropertiesMapper);

internal record struct ComparerMetaDataSet(MethodMetaData identityComparer);