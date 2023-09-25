namespace Oasis.EntityFramework.Mapper.InternalLogic;

using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using Oasis.EntityFramework.Mapper.Exceptions;

internal sealed class ToDatabaseMapper : IToDatabaseMapper
{
    private readonly ToDatabaseRecursiveMapper _mapper;
    private readonly IRecursiveMappingContext _context;

    public ToDatabaseMapper(ToDatabaseRecursiveMapper mapper, IReadOnlyDictionary<Type, ITargetByIdTrackerFactory> targetByIdTrackerFactories, bool isSession, DbContext? databaseContext = null)
    {
        _mapper = mapper;
        _context = new RecursiveMappingContext(targetByIdTrackerFactories, mapper, isSession, databaseContext);
    }

    public DbContext DatabaseContext
    {
        set => _context.DatabaseContext = value;
    }

    public async Task<TTarget> MapAsync<TSource, TTarget>(TSource source, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer)
        where TSource : class
        where TTarget : class
    {
        if (_context.DatabaseContext == null)
        {
            throw new DbContextMissingException();
        }

        return await _mapper.MapAsync(source, includer, _context);
    }
}

internal sealed class ToMemoryMapper : IToMemoryMapper
{
    private readonly ToMemoryRecursiveMapper _mapper;
    private readonly IRecursiveMappingContext _context;

    public ToMemoryMapper(ToMemoryRecursiveMapper mapper, IReadOnlyDictionary<Type, ITargetByIdTrackerFactory> targetByIdTrackerFactories, bool isSession)
    {
        _mapper = mapper;
        _context = new RecursiveMappingContext(targetByIdTrackerFactories, mapper, isSession);
    }

    public TTarget Map<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class
        => _mapper.MapNew<TSource, TTarget>(source, _context);
}

internal sealed class Mapper : IMapper
{
    private readonly ToDatabaseRecursiveMapper _toDatabaseMapper;
    private readonly ToMemoryRecursiveMapper _toMemoryMapper;
    private readonly IRecursiveMappingContext _context;

    public Mapper(
        ToDatabaseRecursiveMapper toDatabaseMapper,
        ToMemoryRecursiveMapper toMemoryMapper,
        IReadOnlyDictionary<Type, ITargetByIdTrackerFactory> targetByIdTrackerFactories,
        DbContext? databaseContext = null)
    {
        _toDatabaseMapper = toDatabaseMapper;
        _toMemoryMapper = toMemoryMapper;

        // context is only using the static content in the mapper, so either _toMemoryMapper or _toDatabaseMapper should be the same here, as they have the same static content
        _context = new RecursiveMappingContext(targetByIdTrackerFactories, _toMemoryMapper, false, databaseContext);
    }

    public DbContext DatabaseContext
    {
        set => _context.DatabaseContext = value;
    }

    public TTarget Map<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class
        => _toMemoryMapper.MapNew<TSource, TTarget>(source, _context);

    public async Task<TTarget> MapAsync<TSource, TTarget>(TSource source, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer)
        where TSource : class
        where TTarget : class
    {
        if (_context.DatabaseContext == null)
        {
            throw new DbContextMissingException();
        }

        return await _toDatabaseMapper.MapAsync(source, includer, _context);
    }
}

internal sealed class MapperFactory : IMapperFactory
{
    private readonly ToMemoryRecursiveMapper _toMemoryRecursiveMapper;
    private readonly ToDatabaseRecursiveMapper _toDatabaseRecursiveMapper;
    private readonly IReadOnlyDictionary<Type, ITargetByIdTrackerFactory> _targetByIdTrackerFactories;

    public MapperFactory(
        KeepUnmatchedManager? keepUnmatchedManager,
        MapToDatabaseTypeManager mapToDatabaseTypeManager,
        MapperSetLookUp lookup,
        EntityTrackerData entityTrackerData,
        EntityHandlerData entityHandlerData)
    {
        _toMemoryRecursiveMapper = new ToMemoryRecursiveMapper(
            lookup, entityTrackerData, entityHandlerData);
        _toDatabaseRecursiveMapper = new ToDatabaseRecursiveMapper(
            keepUnmatchedManager, mapToDatabaseTypeManager, lookup, entityTrackerData, entityHandlerData);
        _targetByIdTrackerFactories = entityTrackerData.targetByIdTrackerFactories;
    }

    public IMapper MakeMapper(DbContext? databaseContext = null) => new Mapper(_toDatabaseRecursiveMapper, _toMemoryRecursiveMapper, _targetByIdTrackerFactories, databaseContext);

    public IToDatabaseMapper MakeToDatabaseMapper(DbContext? databaseContext = null)
        => new ToDatabaseMapper(_toDatabaseRecursiveMapper, _targetByIdTrackerFactories, false, databaseContext);

    public IToDatabaseMapper MakeToDatabaseSession(DbContext? databaseContext = null)
        => new ToDatabaseMapper(_toDatabaseRecursiveMapper, _targetByIdTrackerFactories, true, databaseContext);

    public IToMemoryMapper MakeToMemoryMapper() => new ToMemoryMapper(_toMemoryRecursiveMapper, _targetByIdTrackerFactories, false);

    public IToMemoryMapper MakeToMemorySession() => new ToMemoryMapper(_toMemoryRecursiveMapper, _targetByIdTrackerFactories, true);
}