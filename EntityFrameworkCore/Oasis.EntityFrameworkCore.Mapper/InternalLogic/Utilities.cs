namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

internal static class Utilities
{
    public const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

    public delegate void MapScalarProperties<TSource, TTarget>(TSource source, TTarget target, IScalarTypeConverter converter)
        where TSource : class
        where TTarget : class;

    public delegate void MapEntityProperties<TSource, TTarget, TKeyType>(IEntityPropertyMapper<TKeyType> mapper, TSource source, TTarget target, INewTargetTracker<TKeyType> newTargetTracker)
        where TSource : class
        where TTarget : class
        where TKeyType : struct;

    public delegate void MapListProperties<TSource, TTarget, TKeyType>(IListPropertyMapper<TKeyType> mapper, TSource source, TTarget target, INewTargetTracker<TKeyType> newTargetTracker)
        where TSource : class
        where TTarget : class
        where TKeyType : struct;

    public delegate object GetScalarProperty<TEntity>(TEntity entity)
        where TEntity : class;

    public delegate bool ScalarPropertyIsEmpty<TEntity>(TEntity entity)
        where TEntity : class;

    public delegate bool ScalarPropertiesAreEqual<TSource, TTarget>(TSource source, TTarget target, IScalarTypeConverter converter)
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

internal record struct MapperSet(Delegate? customPropertiesMapper, Delegate keyPropertiesMapper, Delegate scalarPropertiesMapper, Delegate entityPropertiesMapper, Delegate listPropertiesMapper);

// get method is only needed for id, not for concurrency token, so it's nullable here
internal record struct TypeKeyProxyMetaDataSet(MethodMetaData? get, MethodMetaData isEmpty, PropertyInfo property);

// get method is only needed for id, not for concurrency token, so it's nullable here
internal record struct TypeKeyProxy(Delegate? get, Delegate isEmpty, PropertyInfo property);

internal record struct MapperMetaDataSet(Delegate? customPropertiesMapper, MethodMetaData keyPropertiesMapper, MethodMetaData scalarPropertiesMapper, MethodMetaData entityPropertiesMapper, MethodMetaData listPropertiesMapper);