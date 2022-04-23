namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Exceptions;
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

    public async Task<TTarget> MapAsync<TSource, TTarget>(TSource source, DbContext databaseContext, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer = null)
        where TSource : class
        where TTarget : class
    {
        var newEntityTracker = new NewTargetTracker<int>(_entityFactory);
        return await MapperStaticMethods.MapAsync(databaseContext, includer, _entityBaseProxy, newEntityTracker, _scalarConverter, _listTypeConstructor, _entityFactory, _lookup, source);
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

    TTarget IMappingSession.Map<TSource, TTarget>(TSource source)
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

    async Task<TTarget> IMappingToDatabaseSession.MapAsync<TSource, TTarget>(TSource source, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer)
    {
        return await MapperStaticMethods.MapAsync(_databaseContext, includer, _entityBaseProxy, _newEntityTracker, _scalarConverter, _listTypeConstructor, _entityFactory, _lookup, source);
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

        TTarget? target;
        if (!entityBaseProxy.IdIsEmpty(source))
        {
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

            if (target == default)
            {
                throw new EntityNotFoundException(typeof(TTarget), entityBaseProxy.GetId(source));
            }

            new ToDatabaseRecursiveMapper(newEntityTracker, scalarConverter, listTypeConstructor, entityFactory, lookup, entityBaseProxy, databaseContext).Map(source, target, true);
        }
        else
        {
            if (!newEntityTracker.NewTargetIfNotExist(source.GetHashCode(), out target))
            {
                new ToDatabaseRecursiveMapper(newEntityTracker, scalarConverter, listTypeConstructor, entityFactory, lookup, entityBaseProxy, databaseContext).Map(source, target!, false);
                databaseContext.Set<TTarget>().Add(target!);
            }
        }

        return target!;
    }

    private static Expression<Func<TEntity, bool>> BuildIdEqualsExpression<TEntity>(
        IIdPropertyTracker identityPropertyTracker,
        IScalarTypeConverter scalarConverter,
        object? value)
       where TEntity : class
    {
        var parameter = Expression.Parameter(typeof(TEntity), "entity");
        var identityProperty = identityPropertyTracker.GetIdProperty<TEntity>();

        var equal = Expression.Equal(
            Expression.Property(parameter, identityProperty),
            Expression.Constant(scalarConverter.Convert(value, identityProperty.PropertyType)));
        return Expression.Lambda<Func<TEntity, bool>>(equal, parameter);
    }
}

internal sealed class NewTargetTracker<TKeyType>
    where TKeyType : struct
{
    private readonly IDictionary<TKeyType, object> _newTargetDictionary = new Dictionary<TKeyType, object>();
    private readonly IEntityFactory _entityFactory;

    public NewTargetTracker(IEntityFactory entityFactory)
    {
        _entityFactory = entityFactory;
    }

    public bool NewTargetIfNotExist<TTarget>(TKeyType key, out TTarget? target)
        where TTarget : class
    {
        bool result = false;
        object? obj;
        target = default;
        lock (_newTargetDictionary)
        {
            result = _newTargetDictionary.TryGetValue(key, out obj);
            if (!result)
            {
                target = _entityFactory.Make<TTarget>();
                _newTargetDictionary.Add(key, target);
            }
        }

        if (result)
        {
            if (obj is TTarget)
            {
                target = obj as TTarget;
            }
        }

        return result;
    }
}
