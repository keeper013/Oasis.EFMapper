namespace Oasis.EntityFramework.Mapper.InternalLogic;

using Oasis.EntityFramework.Mapper.Exceptions;
using System.Data.Entity;
using System.Linq.Expressions;

internal sealed class Mapper : IMapper
{
    private readonly IScalarTypeConverter _scalarConverter;
    private readonly IListTypeConstructor _listTypeConstructor;
    private readonly IEntityFactory _entityFactory;
    private readonly MapperSetLookUp _lookup;
    private readonly EntityBaseProxy _entityBaseProxy;

    public Mapper(
        IScalarTypeConverter scalarConverter,
        IListTypeConstructor listTypeConstructor,
        IEntityFactory entityFactory,
        MapperSetLookUp lookup,
        EntityBaseProxy entityBaseProxy)
    {
        _scalarConverter = scalarConverter;
        _listTypeConstructor = listTypeConstructor;
        _entityFactory = entityFactory;
        _lookup = lookup;
        _entityBaseProxy = entityBaseProxy;
    }

    public IMappingSession CreateMappingSession()
    {
        return new MappingSession(_scalarConverter, _listTypeConstructor, _entityFactory, _lookup, _entityBaseProxy);
    }

    public IMappingToDatabaseSession CreateMappingToDatabaseSession(DbContext databaseContext)
    {
        return new MappingToDatabaseSession(_scalarConverter, _listTypeConstructor, _entityFactory, _lookup, _entityBaseProxy, databaseContext);
    }

    public TTarget Map<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class
    {
        var newEntityTracker = new NewTargetTracker<int>(_entityFactory);
        return MapperStaticMethods.Map<TSource, TTarget>(_entityFactory, newEntityTracker, _scalarConverter, _listTypeConstructor, _entityBaseProxy, _lookup, source);
    }

    public async Task<TTarget> MapAsync<TSource, TTarget>(TSource source, DbContext databaseContext, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer = null, MapToDatabaseType mappingType = MapToDatabaseType.Upsert)
        where TSource : class
        where TTarget : class
    {
        var newEntityTracker = new NewTargetTracker<int>(_entityFactory);
        return await MapperStaticMethods.MapAsync(databaseContext, includer, mappingType, _entityBaseProxy, newEntityTracker, _scalarConverter, _listTypeConstructor, _entityFactory, _lookup, source);
    }
}

internal sealed class MappingSession : IMappingSession
{
    private readonly IScalarTypeConverter _scalarConverter;
    private readonly IListTypeConstructor _listTypeConstructor;
    private readonly IEntityFactory _entityFactory;
    private readonly MapperSetLookUp _lookup;
    private readonly EntityBaseProxy _entityBaseProxy;
    private readonly NewTargetTracker<int> _newEntityTracker;

    public MappingSession(
        IScalarTypeConverter scalarConverter,
        IListTypeConstructor listTypeConstructor,
        IEntityFactory entityFactory,
        MapperSetLookUp lookup,
        EntityBaseProxy entityBaseProxy)
    {
        _newEntityTracker = new NewTargetTracker<int>(entityFactory);
        _scalarConverter = scalarConverter;
        _listTypeConstructor = listTypeConstructor;
        _entityFactory = entityFactory;
        _lookup = lookup;
        _entityBaseProxy = entityBaseProxy;
    }

    public TTarget Map<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class
    {
        return MapperStaticMethods.Map<TSource, TTarget>(_entityFactory, _newEntityTracker, _scalarConverter, _listTypeConstructor, _entityBaseProxy, _lookup, source);
    }
}

internal sealed class MappingToDatabaseSession : IMappingToDatabaseSession
{
    private readonly IEntityFactory _entityFactory;
    private readonly IScalarTypeConverter _scalarConverter;
    private readonly IListTypeConstructor _listTypeConstructor;
    private readonly MapperSetLookUp _lookup;
    private readonly DbContext _databaseContext;
    private readonly EntityBaseProxy _entityBaseProxy;
    private readonly NewTargetTracker<int> _newEntityTracker;

    public MappingToDatabaseSession(
        IScalarTypeConverter scalarConverter,
        IListTypeConstructor listTypeConstructor,
        IEntityFactory entityFactory,
        MapperSetLookUp lookup,
        EntityBaseProxy entityBaseProxy,
        DbContext databaseContext)
    {
        _entityFactory = entityFactory;
        _entityBaseProxy = entityBaseProxy;
        _databaseContext = databaseContext;
        _newEntityTracker = new NewTargetTracker<int>(entityFactory);
        _scalarConverter = scalarConverter;
        _listTypeConstructor = listTypeConstructor;
        _lookup = lookup;
    }

    public async Task<TTarget> MapAsync<TSource, TTarget>(TSource source, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer, MapToDatabaseType mappingType = MapToDatabaseType.Upsert)
        where TSource : class
        where TTarget : class
    {
        return await MapperStaticMethods.MapAsync(_databaseContext, includer, mappingType, _entityBaseProxy, _newEntityTracker, _scalarConverter, _listTypeConstructor, _entityFactory, _lookup, source);
    }
}

internal static class MapperStaticMethods
{
    public static TTarget Map<TSource, TTarget>(
        IEntityFactory entityFactory,
        NewTargetTracker<int> newEntityTracker,
        IScalarTypeConverter scalarConverter,
        IListTypeConstructor listTypeConstructor,
        EntityBaseProxy entityBaseProxy,
        MapperSetLookUp lookup,
        TSource source)
        where TSource : class
        where TTarget : class
    {
        var target = entityFactory.Make<TTarget>();
        new ToMemoryRecursiveMapper(newEntityTracker, scalarConverter, listTypeConstructor, lookup, entityBaseProxy).Map(source, target, true);

        return target;
    }

    public static async Task<TTarget> MapAsync<TSource, TTarget>(
        DbContext databaseContext,
        Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer,
        MapToDatabaseType mappingType,
        EntityBaseProxy entityBaseProxy,
        NewTargetTracker<int> newEntityTracker,
        IScalarTypeConverter scalarConverter,
        IListTypeConstructor listTypeConstructor,
        IEntityFactory entityFactory,
        MapperSetLookUp lookup,
        TSource source)
        where TSource : class
        where TTarget : class
    {
        const string AsNoTrackingMethodCall = ".AsNoTracking";

        var sourceHasId = entityBaseProxy.HasId<TSource>() && !entityBaseProxy.IdIsEmpty(source);
        if (sourceHasId)
        {
            TTarget? target;
            var identityEqualsExpression = BuildIdEqualsExpression<TTarget>(entityBaseProxy, scalarConverter, entityBaseProxy.GetId(source));
            if (includer != default)
            {
                var includerString = includer.ToString();
                if (includerString.Contains(AsNoTrackingMethodCall))
                {
                    throw new AsNoTrackingNotAllowedException(includerString);
                }

                target = await includer.Compile()(databaseContext.Set<TTarget>()).FirstOrDefaultAsync(identityEqualsExpression);
            }
            else
            {
                target = await databaseContext.Set<TTarget>().FirstOrDefaultAsync(identityEqualsExpression);
            }

            if (target != default)
            {
                if (mappingType == MapToDatabaseType.Insert)
                {
                    throw new InsertToDatabaseWithExistingException();
                }
                else
                {
                    new ToDatabaseRecursiveMapper(newEntityTracker, scalarConverter, listTypeConstructor, entityFactory, lookup, entityBaseProxy, databaseContext).Map(source, target, true);
                    return target;
                }
            }
        }

        if (mappingType == MapToDatabaseType.Update)
        {
            if (!sourceHasId)
            {
                throw new UpdateToDatabaseWithoutIdException();
            }
            else
            {
                throw new UpdateToDatabaseWithoutRecordException();
            }
        }

        if (!newEntityTracker.NewTargetIfNotExist(source.GetHashCode(), out TTarget newTarget))
        {
            new ToDatabaseRecursiveMapper(newEntityTracker, scalarConverter, listTypeConstructor, entityFactory, lookup, entityBaseProxy, databaseContext).Map(source, newTarget, sourceHasId);
            databaseContext.Set<TTarget>().Add(newTarget);
        }

        return newTarget;
    }

    private static Expression<Func<TEntity, bool>> BuildIdEqualsExpression<TEntity>(
        IIdPropertyTracker identityPropertyTracker,
        IScalarTypeConverter scalarConverter,
        object? value)
       where TEntity : class
    {
        var parameter = Expression.Parameter(typeof(TEntity), "entity");
        var identityProperty = identityPropertyTracker.GetIdProperty<TEntity>();

        var equal = identityProperty.PropertyType.IsInstanceOfType(value) ?
            Expression.Equal(
                Expression.Property(parameter, identityProperty),
                Expression.Convert(Expression.Constant(value), identityProperty.PropertyType))
            : Expression.Equal(
                Expression.Property(parameter, identityProperty),
                Expression.Constant(scalarConverter.Convert(value, identityProperty.PropertyType)));
        return Expression.Lambda<Func<TEntity, bool>>(equal, parameter);
    }
}

internal sealed class NewTargetTracker<TKeyType>
    where TKeyType : struct
{
    private readonly IDictionary<TKeyType, IDictionary<Type, object>> _newTargetDictionary = new Dictionary<TKeyType, IDictionary<Type, object>>();
    private readonly IEntityFactory _entityFactory;

    public NewTargetTracker(IEntityFactory entityFactory)
    {
        _entityFactory = entityFactory;
    }

    public bool NewTargetIfNotExist<TTarget>(TKeyType key, out TTarget target)
        where TTarget : class
    {
        IDictionary<Type, object>? innerDictionary;
        lock (_newTargetDictionary)
        {
            var targetType = typeof(TTarget);
            if (_newTargetDictionary.TryGetValue(key, out innerDictionary) && innerDictionary.TryGetValue(targetType, out var obj))
            {
                target = (TTarget)obj!;
                return true;
            }
            else
            {
                if (innerDictionary == default)
                {
                    innerDictionary = new Dictionary<Type, object>();
                    _newTargetDictionary.Add(key, innerDictionary);
                }

                target = _entityFactory.Make<TTarget>();
                innerDictionary.Add(targetType, target);
                return false;
            }
        }
    }
}
