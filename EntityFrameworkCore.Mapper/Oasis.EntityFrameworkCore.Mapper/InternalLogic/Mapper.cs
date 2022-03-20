namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Linq.Expressions;

internal sealed class Mapper : IMapper
{
    private readonly IScalarTypeConverter _scalarConverter;
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> _mappers;
    private readonly EntityBaseProxy _entityBaseProxy;

    public Mapper(
        IScalarTypeConverter scalarConverter,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> mappers,
        EntityBaseProxy entityBaseProxy)
    {
        _scalarConverter = scalarConverter;
        _mappers = mappers;
        _entityBaseProxy = entityBaseProxy;
    }

    public IMappingSession CreateMappingSession()
    {
        return new MappingSession(_scalarConverter, _mappers, _entityBaseProxy);
    }

    public IMappingToDatabaseSession CreateMappingToDatabaseSession(DbContext databaseContext)
    {
        return new MappingToDatabaseSession(_scalarConverter, _mappers, _entityBaseProxy, databaseContext);
    }
}

internal sealed class MappingSession : IMappingSession
{
    private readonly IScalarTypeConverter _scalarConverter;
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> _mappers;
    private readonly EntityBaseProxy _entityBaseProxy;
    private readonly NewTargetTracker<int> _newEntityTracker;

    public MappingSession(
        IScalarTypeConverter scalarConverter,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> mappers,
        EntityBaseProxy entityBaseProxy)
    {
        _newEntityTracker = new NewTargetTracker<int>();
        _scalarConverter = scalarConverter;
        _mappers = mappers;
        _entityBaseProxy = entityBaseProxy;
    }

    TTarget IMappingSession.Map<TSource, TTarget>(TSource source)
    {
        if (!_entityBaseProxy.IdIsEmpty(source))
        {
            if (_entityBaseProxy.TimeStampIsEmpty(source))
            {
                throw new MissingTimestampException(typeof(TSource), _entityBaseProxy.GetId(source));
            }
        }

        var target = new TTarget();
        new ToMemoryRecursiveMapper(_newEntityTracker, _scalarConverter, _mappers, _entityBaseProxy).Map(source, target);

        return target;
    }
}

internal sealed class MappingToDatabaseSession : IMappingToDatabaseSession
{
    private readonly IScalarTypeConverter _scalarConverter;
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> _mappers;
    private readonly DbContext _databaseContext;
    private readonly EntityBaseProxy _entityBaseProxy;
    private readonly NewTargetTracker<int> _newEntityTracker;

    public MappingToDatabaseSession(
        IScalarTypeConverter scalarConverter,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> mappers,
        EntityBaseProxy entityBaseProxy,
        DbContext databaseContext)
    {
        _entityBaseProxy = entityBaseProxy;
        _databaseContext = databaseContext;
        _newEntityTracker = new NewTargetTracker<int>();
        _scalarConverter = scalarConverter;
        _mappers = mappers;
    }

    async Task<TTarget> IMappingToDatabaseSession.MapAsync<TSource, TTarget>(TSource source, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer)
    {
        const string AsNoTrackingMethodCall = ".AsNoTracking";

        TTarget? target;
        if (!_entityBaseProxy.IdIsEmpty(source))
        {
            var identityEqualsExpression = Utilities.BuildIdEqualsExpression<TTarget>(_entityBaseProxy, _entityBaseProxy.GetId(source));
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

            if (_entityBaseProxy.TimeStampIsEmpty(target))
            {
                throw new MissingTimestampException(typeof(TTarget), _entityBaseProxy.GetId(source));
            }

            if (!_entityBaseProxy.TimeStampEquals(source, target))
            {
                throw new StaleEntityException(typeof(TTarget), _entityBaseProxy.GetId(source));
            }

            new ToDatabaseRecursiveMapper(_newEntityTracker, _scalarConverter, _mappers, _entityBaseProxy, _databaseContext).Map(source, target);
        }
        else
        {
            if (!_newEntityTracker.NewTargetIfNotExist(source.GetHashCode(), out target))
            {
                new ToDatabaseRecursiveMapper(_newEntityTracker, _scalarConverter, _mappers, _entityBaseProxy, _databaseContext).Map(source, target!);
                _databaseContext.Set<TTarget>().Add(target!);
            }
        }

        return target!;
    }
}

internal sealed class NewTargetTracker<TKeyType>
    where TKeyType : struct
{
    private readonly IDictionary<TKeyType, object> _newTargetDictionary = new Dictionary<TKeyType, object>();

    public bool NewTargetIfNotExist<TTarget>(TKeyType key, out TTarget? target)
        where TTarget : class, new()
    {
        bool result = false;
        object? obj;
        target = default;
        lock (_newTargetDictionary)
        {
            result = _newTargetDictionary.TryGetValue(key, out obj);
            if (!result)
            {
                target = new TTarget();
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
