namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using System.Linq.Expressions;
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

    public delegate bool TimeStampIsEmpty<TEntity>(TEntity entity)
        where TEntity : class;

    public delegate bool IdsAreEqual<TSource, TTarget>(TSource source, TTarget target, IScalarTypeConverter converter)
        where TSource : class
        where TTarget : class;

    public delegate bool TimeStampsAreEqual<TSource, TTarget>(TSource source, TTarget target, IScalarTypeConverter converter)
        where TSource : class
        where TTarget : class;

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

internal record struct MapperMetaDataSet(MethodMetaData scalarPropertiesMapper, MethodMetaData entityPropertiesMapper, MethodMetaData listPropertiesMapper);

// TODO: timestamp property may not exist
internal record struct ComparerMetaDataSet(MethodMetaData identityComparer, MethodMetaData timeStampComparer);