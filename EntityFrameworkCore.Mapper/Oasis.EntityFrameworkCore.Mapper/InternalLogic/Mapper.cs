namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Linq.Expressions;

internal sealed class Mapper : IMapper
{
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> _scalarConverters;
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> _mappers;
    private readonly EntityBaseProxy _entityBaseProxy;

    public Mapper(
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> scalarConverters,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> mappers,
        EntityBaseProxy entityBaseProxy)
    {
        _scalarConverters = scalarConverters;
        _mappers = mappers;
        _entityBaseProxy = entityBaseProxy;
    }

    public IMappingSession CreateMappingSession()
    {
        return new MappingSession(_scalarConverters, _mappers);
    }

    public IMappingToDatabaseSession CreateMappingToDatabaseSession(DbContext databaseContext)
    {
        return new MappingToDatabaseSession(_scalarConverters, _mappers, _entityBaseProxy, databaseContext);
    }
}

internal sealed class MappingSession : IMappingSession
{
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> _scalarConverters;
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> _mappers;
    private readonly NewTargetTracker<int> _newEntityTracker;

    public MappingSession(
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> scalarConverters,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> mappers)
    {
        _newEntityTracker = new NewTargetTracker<int>();
        _scalarConverters = scalarConverters;
        _mappers = mappers;
    }

    TTarget IMappingSession.Map<TSource, TTarget>(TSource source)
    {
        if (source.Id != default)
        {
            if (source.Timestamp == default)
            {
                throw new MissingTimestampException(typeof(TSource), source.Id);
            }
        }

        var target = new TTarget();
        new ToMemoryRecursiveMapper(_newEntityTracker, _scalarConverters, _mappers).Map(source, target);

        return target;
    }
}

internal sealed class MappingToDatabaseSession : IMappingToDatabaseSession
{
    private const string AsNoTrackingMethodCall = ".AsNoTracking";
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> _scalarConverters;
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> _mappers;
    private readonly DbContext _databaseContext;
    private readonly EntityBaseProxy _entityBaseProxy;
    private readonly NewTargetTracker<int> _newEntityTracker;

    public MappingToDatabaseSession(
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> scalarConverters,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> mappers,
        EntityBaseProxy entityBaseProxy,
        DbContext databaseContext)
    {
        _entityBaseProxy = entityBaseProxy;
        _databaseContext = databaseContext;
        _newEntityTracker = new NewTargetTracker<int>();
        _scalarConverters = scalarConverters;
        _mappers = mappers;
    }

    async Task<TTarget> IMappingToDatabaseSession.MapAsync<TSource, TTarget>(TSource source, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer)
    {
        TTarget? target;
        if (source.Id != default)
        {
            if (includer != default)
            {
                var includerString = includer.ToString();
                if (includerString.Contains(AsNoTrackingMethodCall))
                {
                    throw new AsNoTrackingNotAllowedException(includerString);
                }

                target = await includer.Compile()(_databaseContext.Set<TTarget>()).SingleOrDefaultAsync(t => t.Id == source.Id);
            }
            else
            {
                target = await _databaseContext.Set<TTarget>().SingleOrDefaultAsync(t => t.Id == source.Id);
            }

            if (target == default)
            {
                throw new EntityNotFoundException(typeof(TTarget), source.Id);
            }

            if (target.Timestamp == default)
            {
                throw new MissingTimestampException(typeof(TTarget), source.Id);
            }

            if (!Enumerable.SequenceEqual(target.Timestamp!, source.Timestamp!))
            {
                throw new StaleEntityException(typeof(TTarget), source.Id);
            }

            new ToDatabaseRecursiveMapper(_newEntityTracker, _scalarConverters, _mappers, _entityBaseProxy, _databaseContext).Map(source, target);
        }
        else
        {
            if (!_newEntityTracker.NewTargetIfNotExist(source.GetHashCode(), out target))
            {
                new ToDatabaseRecursiveMapper(_newEntityTracker, _scalarConverters, _mappers, _entityBaseProxy, _databaseContext).Map(source, target!);
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
