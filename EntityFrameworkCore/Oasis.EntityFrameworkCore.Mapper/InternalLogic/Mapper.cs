namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

internal sealed class Mapper : IMapper
{
    private readonly IScalarTypeConverter _scalarConverter;
    private readonly IListTypeConstructor _listTypeConstructor;
    private readonly IEntityFactory _entityFactory;
    private readonly MapperSetLookUp _lookup;
    private readonly EntityBaseProxy _entityBaseProxy;
    private readonly TargetTrackerProvider _targetTrackerProvider;
    private readonly ToMemoryRecursiveMapper _toMemoryRecursiveMapper;

    public Mapper(
        IScalarTypeConverter scalarConverter,
        IListTypeConstructor listTypeConstructor,
        MapperSetLookUp lookup,
        EntityBaseProxy entityBaseProxy,
        TargetTrackerProvider targetTrackerProvider,
        IEntityFactory entityFactory)
    {
        _scalarConverter = scalarConverter;
        _listTypeConstructor = listTypeConstructor;
        _entityFactory = entityFactory;
        _lookup = lookup;
        _entityBaseProxy = entityBaseProxy;
        _targetTrackerProvider = targetTrackerProvider;
        _toMemoryRecursiveMapper = new ToMemoryRecursiveMapper(scalarConverter, listTypeConstructor, lookup, entityBaseProxy, entityFactory);
    }

    public IMappingSession CreateMappingSession()
    {
        return new MappingSession(_entityFactory, _targetTrackerProvider, _toMemoryRecursiveMapper);
    }

    public IMappingToDatabaseSession CreateMappingToDatabaseSession(DbContext databaseContext)
    {
        return new MappingToDatabaseSession(_scalarConverter, _listTypeConstructor, _lookup, _entityBaseProxy, _entityFactory, _targetTrackerProvider, databaseContext);
    }

    public TTarget Map<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class
    {
        var newEntityTracker = _targetTrackerProvider.Provide<TSource, TTarget, int>();
        var target = _entityFactory.Make<TTarget>();
        _toMemoryRecursiveMapper.Map(source, target, true, newEntityTracker);
        return target;
    }

    public async Task<TTarget> MapAsync<TSource, TTarget>(TSource source, DbContext databaseContext, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer = default, MapToDatabaseType mappingType = MapToDatabaseType.Upsert)
        where TSource : class
        where TTarget : class
    {
        var newEntityTracker = _targetTrackerProvider.Provide<TSource, TTarget, int>();
        var toDatabaseRecursiveMapper = new ToDatabaseRecursiveMapper(_scalarConverter, _listTypeConstructor, _lookup, _entityBaseProxy, _entityFactory, databaseContext);
        return await toDatabaseRecursiveMapper.MapAsync(source, includer, mappingType, newEntityTracker);
    }
}

internal sealed class MappingSession : IMappingSession
{
    private readonly IEntityFactory _entityFactory;
    private readonly INewTargetTracker<int>? _entityTracker;
    private readonly ToMemoryRecursiveMapper _toMemoryRecursiveMapper;

    public MappingSession(
        IEntityFactory entityFactory,
        TargetTrackerProvider targetTrackerProvider,
        ToMemoryRecursiveMapper toMemoryRecursiveMapper)
    {
        _entityFactory = entityFactory;
        _entityTracker = targetTrackerProvider.Provide<int>();
        _toMemoryRecursiveMapper = toMemoryRecursiveMapper;
    }

    public TTarget Map<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class
    {
        var target = _entityFactory.Make<TTarget>();
        _toMemoryRecursiveMapper.Map(source, target, true, _entityTracker);

        return target;
    }
}

internal sealed class MappingToDatabaseSession : IMappingToDatabaseSession
{
    private readonly INewTargetTracker<int>? _newEntityTracker;
    private readonly ToDatabaseRecursiveMapper _toDatabaseRecursiveMapper;

    public MappingToDatabaseSession(
        IScalarTypeConverter scalarConverter,
        IListTypeConstructor listTypeConstructor,
        MapperSetLookUp lookup,
        EntityBaseProxy entityBaseProxy,
        IEntityFactory entityFactory,
        TargetTrackerProvider targetTrackerProvider,
        DbContext databaseContext)
    {
        _newEntityTracker = targetTrackerProvider.Provide<int>();
        _toDatabaseRecursiveMapper = new ToDatabaseRecursiveMapper(scalarConverter, listTypeConstructor, lookup, entityBaseProxy, entityFactory, databaseContext);
    }

    public async Task<TTarget> MapAsync<TSource, TTarget>(TSource source, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer = default, MapToDatabaseType mappingType = MapToDatabaseType.Upsert)
        where TSource : class
        where TTarget : class
    {
        return await _toDatabaseRecursiveMapper.MapAsync(source, includer, mappingType, _newEntityTracker);
    }
}

internal sealed class TargetTrackerProvider
{
    private readonly IReadOnlyDictionary<Type, ISet<Type>> _loopDependencyMappings;
    private readonly IEntityFactory _entityFactory;

    public TargetTrackerProvider(IReadOnlyDictionary<Type, ISet<Type>> loopDependencyMappings, IEntityFactory entityFactory)
    {
        _loopDependencyMappings = loopDependencyMappings;
        _entityFactory = entityFactory;
    }

    public INewTargetTracker<TKeyType>? Provide<TSource, TTarget, TKeyType>()
        where TSource : class
        where TTarget : class
        where TKeyType : struct
    {
        return _loopDependencyMappings.TryGetValue(typeof(TSource), out var set) && set.Contains(typeof(TTarget))
            ? new NewTargetTracker<TKeyType>(_entityFactory) : null;
    }

    public INewTargetTracker<TKeyType> Provide<TKeyType>()
        where TKeyType : struct
    {
        return new NewTargetTracker<TKeyType>(_entityFactory);
    }

    private sealed class NewTargetTracker<TKeyType> : INewTargetTracker<TKeyType>
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
}