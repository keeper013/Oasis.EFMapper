namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Linq.Expressions;
using System.Reflection;

internal sealed class Mapper : IMapper
{
    private readonly IScalarTypeConverter _scalarConverter;
    private readonly IEntityFactory _entityFactory;
    private readonly MapperSetLookUp _lookup;
    private readonly EntityBaseProxy _entityBaseProxy;

    public Mapper(
        IScalarTypeConverter scalarConverter,
        IEntityFactory entityFactory,
        MapperSetLookUp lookup,
        EntityBaseProxy entityBaseProxy)
    {
        _scalarConverter = scalarConverter;
        _entityFactory = entityFactory;
        _lookup = lookup;
        _entityBaseProxy = entityBaseProxy;
    }

    public IMappingSession CreateMappingSession()
    {
        return new MappingSession(_scalarConverter, _entityFactory, _lookup, _entityBaseProxy);
    }

    public IMappingToDatabaseSession CreateMappingToDatabaseSession(DbContext databaseContext)
    {
        return new MappingToDatabaseSession(_scalarConverter, _entityFactory, _lookup, _entityBaseProxy, databaseContext);
    }
}

internal sealed class MappingSession : IMappingSession
{
    private readonly IScalarTypeConverter _scalarConverter;
    private readonly IEntityFactory _entityFactory;
    private readonly MapperSetLookUp _lookup;
    private readonly EntityBaseProxy _entityBaseProxy;
    private readonly NewTargetTracker<int> _newEntityTracker;

    public MappingSession(
        IScalarTypeConverter scalarConverter,
        IEntityFactory entityFactory,
        MapperSetLookUp lookup,
        EntityBaseProxy entityBaseProxy)
    {
        _newEntityTracker = new NewTargetTracker<int>(entityFactory);
        _scalarConverter = scalarConverter;
        _entityFactory = entityFactory;
        _lookup = lookup;
        _entityBaseProxy = entityBaseProxy;
    }

    TTarget IMappingSession.Map<TSource, TTarget>(TSource source)
    {
        var target = _entityFactory.Make<TTarget>();
        new ToMemoryRecursiveMapper(_newEntityTracker, _scalarConverter, _lookup, _entityBaseProxy).Map(source, target, true);

        return target;
    }
}

internal sealed class MappingToDatabaseSession : IMappingToDatabaseSession
{
    private readonly IEntityFactory _entityFactory;
    private readonly IScalarTypeConverter _scalarConverter;
    private readonly MapperSetLookUp _lookup;
    private readonly DbContext _databaseContext;
    private readonly EntityBaseProxy _entityBaseProxy;
    private readonly NewTargetTracker<int> _newEntityTracker;

    public MappingToDatabaseSession(
        IScalarTypeConverter scalarConverter,
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
        _lookup = lookup;
    }

    async Task<TTarget> IMappingToDatabaseSession.MapAsync<TSource, TTarget>(TSource source, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer)
    {
        const string AsNoTrackingMethodCall = ".AsNoTracking";

        TTarget? target;
        if (!_entityBaseProxy.IdIsEmpty(source))
        {
            var identityEqualsExpression = BuildIdEqualsExpression<TTarget>(_entityBaseProxy, _entityBaseProxy.GetId(source));
            if (includer != default)
            {
                var includerString = includer.ToString();
                if (includerString.Contains(AsNoTrackingMethodCall))
                {
                    throw new AsNoTrackingNotAllowedException(includerString);
                }

                target = await includer.Compile()(_databaseContext.Set<TTarget>()).SingleOrDefaultAsync(identityEqualsExpression);
            }
            else
            {
                target = await _databaseContext.Set<TTarget>().SingleOrDefaultAsync(identityEqualsExpression);
            }

            if (target == default)
            {
                throw new EntityNotFoundException(typeof(TTarget), _entityBaseProxy.GetId(source));
            }

            new ToDatabaseRecursiveMapper(_newEntityTracker, _scalarConverter, _entityFactory, _lookup, _entityBaseProxy, _databaseContext).Map(source, target, true);
        }
        else
        {
            if (!_newEntityTracker.NewTargetIfNotExist(source.GetHashCode(), out target))
            {
                new ToDatabaseRecursiveMapper(_newEntityTracker, _scalarConverter, _entityFactory, _lookup, _entityBaseProxy, _databaseContext).Map(source, target!, false);
                _databaseContext.Set<TTarget>().Add(target!);
            }
        }

        return target!;
    }

    private Expression<Func<TEntity, bool>> BuildIdEqualsExpression<TEntity>(IIdPropertyTracker identityPropertyTracker, object? value)
        where TEntity : class
    {
        var parameter = Expression.Parameter(typeof(TEntity), "entity");
        var identityProperty = identityPropertyTracker.GetIdProperty<TEntity>();

        var equal = Expression.Equal(
            Expression.Property(parameter, identityProperty),
            Expression.Constant(_scalarConverter.Convert(value, identityProperty.PropertyType)));
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
