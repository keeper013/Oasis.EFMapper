namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

internal sealed class Mapper : IMapper
{
    private readonly IScalarTypeConverter _scalarConverter;
    private readonly IListTypeConstructor _listTypeConstructor;
    private readonly MapperSetLookUp _lookup;
    private readonly EntityHandler _entityHandler;
    private readonly KeepUnmatchedManager? _keepUnmatchedManager;
    private readonly MapToDatabaseTypeManager _mapToDatabaseTypeManager;
    private readonly ToMemoryRecursiveMapper _toMemoryRecursiveMapper;
    private readonly ToDatabaseRecursiveMapper _toDatabaseRecursiveMapper;
    private readonly RecursiveMappingContextFactory _contextFactory;
    private readonly IRecursiveMappingContext _context;

    public Mapper(
        IScalarTypeConverter scalarConverter,
        IListTypeConstructor listTypeConstructor,
        MapperSetLookUp lookup,
        EntityHandler entityHandler,
        KeepUnmatchedManager? keepUnmatchedManager,
        MapToDatabaseTypeManager mapToDatabaseTypeManager,
        RecursiveMappingContextFactory contextFactory)
    {
        _scalarConverter = scalarConverter;
        _listTypeConstructor = listTypeConstructor;
        _lookup = lookup;
        _entityHandler = entityHandler;
        _keepUnmatchedManager = keepUnmatchedManager;
        _mapToDatabaseTypeManager = mapToDatabaseTypeManager;
        _contextFactory = contextFactory;
        _toMemoryRecursiveMapper = new ToMemoryRecursiveMapper(scalarConverter, listTypeConstructor, lookup, entityHandler);
        _toDatabaseRecursiveMapper = new ToDatabaseRecursiveMapper(scalarConverter, listTypeConstructor, lookup, entityHandler, keepUnmatchedManager, mapToDatabaseTypeManager);
        _context = _contextFactory.Make(false);
    }

    public IMappingSession CreateMappingSession()
    {
        return new MappingSession(_entityHandler, _contextFactory.Make(true), _toMemoryRecursiveMapper);
    }

    public IMappingToDatabaseSession CreateMappingToDatabaseSession(DbContext databaseContext)
    {
        return new MappingToDatabaseSession(_scalarConverter, _listTypeConstructor, _lookup, _entityHandler, _contextFactory.Make(true), _keepUnmatchedManager, _mapToDatabaseTypeManager, databaseContext);
    }

    public TTarget Map<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class
    {
        return _context.TrackIfNecessaryAndMap(source, null, _entityHandler.Make<TTarget>, _toMemoryRecursiveMapper.Map<TSource, TTarget>, false);
    }

    public async Task<TTarget> MapAsync<TSource, TTarget>(TSource source, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer, DbContext databaseContext)
        where TSource : class
        where TTarget : class
    {
        var tracker = _context.GetTracker<TSource, TTarget>(source);
        _toDatabaseRecursiveMapper.DatabaseContext = databaseContext;
        var target = await _toDatabaseRecursiveMapper.MapAsync(source, includer, tracker, _context);
        _context.Clear();
        return target;
    }
}

internal sealed class MappingSession : IMappingSession
{
    private readonly IRecursiveMappingContext _context;
    private readonly ToMemoryRecursiveMapper _toMemoryRecursiveMapper;
    private readonly EntityHandler _entityHandler;

    public MappingSession(
        EntityHandler entityHandler,
        IRecursiveMappingContext context,
        ToMemoryRecursiveMapper toMemoryRecursiveMapper)
    {
        _context = context;
        _entityHandler = entityHandler;
        _toMemoryRecursiveMapper = toMemoryRecursiveMapper;
    }

    public TTarget Map<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class
    {
        return _context.TrackIfNecessaryAndMap(source, null, _entityHandler.Make<TTarget>, _toMemoryRecursiveMapper.Map<TSource, TTarget>, true);
    }
}

internal sealed class MappingToDatabaseSession : IMappingToDatabaseSession
{
    private readonly IRecursiveMappingContext _context;
    private readonly ToDatabaseRecursiveMapper _toDatabaseRecursiveMapper;

    public MappingToDatabaseSession(
        IScalarTypeConverter scalarConverter,
        IListTypeConstructor listTypeConstructor,
        MapperSetLookUp lookup,
        EntityHandler entityHandler,
        IRecursiveMappingContext context,
        KeepUnmatchedManager? keepUnmatchedManager,
        MapToDatabaseTypeManager mapToDatabaseTypeManager,
        DbContext databaseContext)
    {
        _context = context;
        _toDatabaseRecursiveMapper = new ToDatabaseRecursiveMapper(scalarConverter, listTypeConstructor, lookup, entityHandler, keepUnmatchedManager, mapToDatabaseTypeManager)
        {
            DatabaseContext = databaseContext,
        };
    }

    public async Task<TTarget> MapAsync<TSource, TTarget>(TSource source, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer)
        where TSource : class
        where TTarget : class
    {
        var trackedTarget = _context.GetTracked<TSource, TTarget>(source, out var tracker);
        if (trackedTarget != null)
        {
            return trackedTarget;
        }

        return await _toDatabaseRecursiveMapper.MapAsync(source, includer, tracker!, _context);
    }
}