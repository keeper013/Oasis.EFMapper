namespace Oasis.EntityFramework.Mapper.InternalLogic;

using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;

internal enum MapKeyProperties : byte
{
    /// <summary>
    /// Mapping neither id nor concurrency token
    /// </summary>
    None = 0,

    /// <summary>
    /// Mapping id only
    /// </summary>
    IdOnly = 1,

    /// <summary>
    /// Mapping both id and concurrency token
    /// </summary>
    IdAndConcurrencyToken = 2,
}

internal sealed class Mapper : IMapper
{
    private readonly IScalarTypeConverter _scalarConverter;
    private readonly IListTypeConstructor _listTypeConstructor;
    private readonly IEntityFactory _entityFactory;
    private readonly MapperSetLookUp _lookup;
    private readonly ExistingTargetTrackerFactory _existingTargetTrackerFactory;
    private readonly EntityBaseProxy _entityBaseProxy;
    private readonly NewTargetTrackerProvider _newTargetTrackerProvider;
    private readonly DependentPropertyManager _dependentPropertyManager;
    private readonly KeepUnmatchedManager _keepUnmatchedManager;
    private readonly MapToDatabaseTypeManager _mapToDatabaseTypeManager;
    private readonly ToMemoryRecursiveMapper _toMemoryRecursiveMapper;

    public Mapper(
        IScalarTypeConverter scalarConverter,
        IListTypeConstructor listTypeConstructor,
        MapperSetLookUp lookup,
        ExistingTargetTrackerFactory existingTargetTrackerFactory,
        EntityBaseProxy entityBaseProxy,
        NewTargetTrackerProvider newTargetTrackerProvider,
        DependentPropertyManager dependentPropertyManager,
        KeepUnmatchedManager keepUnmatchedManager,
        MapToDatabaseTypeManager mapToDatabaseTypeManager,
        IEntityFactory entityFactory)
    {
        _scalarConverter = scalarConverter;
        _listTypeConstructor = listTypeConstructor;
        _entityFactory = entityFactory;
        _lookup = lookup;
        _existingTargetTrackerFactory = existingTargetTrackerFactory;
        _entityBaseProxy = entityBaseProxy;
        _newTargetTrackerProvider = newTargetTrackerProvider;
        _dependentPropertyManager = dependentPropertyManager;
        _keepUnmatchedManager = keepUnmatchedManager;
        _mapToDatabaseTypeManager = mapToDatabaseTypeManager;
        _toMemoryRecursiveMapper = new ToMemoryRecursiveMapper(scalarConverter, listTypeConstructor, lookup, entityBaseProxy, entityFactory);
    }

    public IMappingSession CreateMappingSession()
    {
        return new MappingSession(_existingTargetTrackerFactory.Make(), _newTargetTrackerProvider.Provide<int>(), _toMemoryRecursiveMapper);
    }

    public IMappingToDatabaseSession CreateMappingToDatabaseSession(DbContext databaseContext)
    {
        return new MappingToDatabaseSession(_scalarConverter, _listTypeConstructor, _lookup, _existingTargetTrackerFactory.Make(), _entityBaseProxy, _dependentPropertyManager, _keepUnmatchedManager, _mapToDatabaseTypeManager, _entityFactory, _newTargetTrackerProvider.Provide<int>(), databaseContext);
    }

    public TTarget Map<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class
    {
        var newEntityTracker = _newTargetTrackerProvider.Provide<TSource, TTarget, int>();
        var existingTargetTracker = _existingTargetTrackerFactory.Make<TTarget>();
        var target = _entityFactory.Make<TTarget>();
        _toMemoryRecursiveMapper.Map(source, target, MapKeyProperties.IdAndConcurrencyToken, existingTargetTracker, newEntityTracker);
        return target;
    }

    public async Task<TTarget> MapAsync<TSource, TTarget>(TSource source, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer, DbContext databaseContext)
        where TSource : class
        where TTarget : class
    {
        var newTargetTracker = _newTargetTrackerProvider.Provide<TSource, TTarget, int>();
        var existingTargetTracker = _existingTargetTrackerFactory.Make<TTarget>();
        var toDatabaseRecursiveMapper = new ToDatabaseRecursiveMapper(_scalarConverter, _listTypeConstructor, _lookup, _entityBaseProxy, _dependentPropertyManager, _keepUnmatchedManager, _mapToDatabaseTypeManager, _entityFactory, databaseContext);
        return await toDatabaseRecursiveMapper.MapAsync(source, includer, existingTargetTracker, newTargetTracker);
    }
}

internal sealed class MappingSession : IMappingSession
{
    private readonly INewTargetTracker<int> _newTargetTracker;
    private readonly IExistingTargetTracker _existingTargetTracker;
    private readonly ToMemoryRecursiveMapper _toMemoryRecursiveMapper;

    public MappingSession(
        IExistingTargetTracker existingTargetTracker,
        INewTargetTracker<int> newTargetTracker,
        ToMemoryRecursiveMapper toMemoryRecursiveMapper)
    {
        _existingTargetTracker = existingTargetTracker;
        _newTargetTracker = newTargetTracker;
        _toMemoryRecursiveMapper = toMemoryRecursiveMapper;
    }

    public TTarget Map<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class
    {
        if (!_newTargetTracker.NewTargetIfNotExist<TTarget>(source.GetHashCode(), out var target))
        {
            _toMemoryRecursiveMapper.Map(source, target, MapKeyProperties.IdAndConcurrencyToken, _existingTargetTracker, _newTargetTracker);
        }

        return target;
    }
}

internal sealed class MappingToDatabaseSession : IMappingToDatabaseSession
{
    private readonly IExistingTargetTracker _existingTargetTracker;
    private readonly INewTargetTracker<int> _newTargetTracker;
    private readonly ToDatabaseRecursiveMapper _toDatabaseRecursiveMapper;

    public MappingToDatabaseSession(
        IScalarTypeConverter scalarConverter,
        IListTypeConstructor listTypeConstructor,
        MapperSetLookUp lookup,
        IExistingTargetTracker existingTargetTracker,
        EntityBaseProxy entityBaseProxy,
        DependentPropertyManager dependentPropertyManager,
        KeepUnmatchedManager keepUnmatchedManager,
        MapToDatabaseTypeManager mapToDatabaseTypeManager,
        IEntityFactory entityFactory,
        INewTargetTracker<int> newTargetTracker,
        DbContext databaseContext)
    {
        _existingTargetTracker = existingTargetTracker;
        _newTargetTracker = newTargetTracker;
        _toDatabaseRecursiveMapper = new ToDatabaseRecursiveMapper(scalarConverter, listTypeConstructor, lookup, entityBaseProxy, dependentPropertyManager, keepUnmatchedManager, mapToDatabaseTypeManager, entityFactory, databaseContext);
    }

    public async Task<TTarget> MapAsync<TSource, TTarget>(TSource source, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer)
        where TSource : class
        where TTarget : class
    {
        return await _toDatabaseRecursiveMapper.MapAsync(source, includer, _existingTargetTracker, _newTargetTracker);
    }
}

internal sealed class NewTargetTrackerProvider
{
    private readonly IReadOnlyDictionary<Type, ISet<Type>> _loopDependencyMappings;
    private readonly IEntityFactory _entityFactory;

    public NewTargetTrackerProvider(IReadOnlyDictionary<Type, ISet<Type>> loopDependencyMappings, IEntityFactory entityFactory)
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