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
    private readonly MapperSetLookUp _lookup;
    private readonly EntityHandler _entityHandler;
    private readonly KeepUnmatchedManager _keepUnmatchedManager;
    private readonly MapToDatabaseTypeManager _mapToDatabaseTypeManager;
    private readonly ToMemoryRecursiveMapper _toMemoryRecursiveMapper;
    private readonly RecursiveMappingContextFactory _contextFactory;

    public Mapper(
        IScalarTypeConverter scalarConverter,
        IListTypeConstructor listTypeConstructor,
        MapperSetLookUp lookup,
        EntityHandler entityHandler,
        KeepUnmatchedManager keepUnmatchedManager,
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
    }

    public IMappingSession CreateMappingSession()
    {
        return new MappingSession(_entityHandler, _contextFactory.Make(), _toMemoryRecursiveMapper);
    }

    public IMappingToDatabaseSession CreateMappingToDatabaseSession(DbContext databaseContext)
    {
        return new MappingToDatabaseSession(_scalarConverter, _listTypeConstructor, _lookup, _entityHandler, _contextFactory.Make(), _keepUnmatchedManager, _mapToDatabaseTypeManager, databaseContext);
    }

    public TTarget Map<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class
    {
        var context = _contextFactory.Make();
        var target = context.GetTracked<TSource, TTarget>(source, out var tracker);
        if (target == null)
        {
            target = _entityHandler.Make<TTarget>();
            tracker!.Track(target);
            _toMemoryRecursiveMapper.Map(source, target, MapKeyProperties.IdAndConcurrencyToken, context);
        }

        return target;
    }

    public async Task<TTarget> MapAsync<TSource, TTarget>(TSource source, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer, DbContext databaseContext)
        where TSource : class
        where TTarget : class
    {
        var toDatabaseRecursiveMapper = new ToDatabaseRecursiveMapper(_scalarConverter, _listTypeConstructor, _lookup, _entityHandler, _keepUnmatchedManager, _mapToDatabaseTypeManager, databaseContext);
        return await toDatabaseRecursiveMapper.MapAsync(source, includer, _contextFactory.Make());
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
        var target = _context.GetTracked<TSource, TTarget>(source, out var tracker);
        if (target == null)
        {
            target = _entityHandler.Make<TTarget>();
            tracker!.Track(target);
            _toMemoryRecursiveMapper.Map(source, target, MapKeyProperties.IdAndConcurrencyToken, _context);
        }

        return target;
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
        KeepUnmatchedManager keepUnmatchedManager,
        MapToDatabaseTypeManager mapToDatabaseTypeManager,
        DbContext databaseContext)
    {
        _context = context;
        _toDatabaseRecursiveMapper = new ToDatabaseRecursiveMapper(scalarConverter, listTypeConstructor, lookup, entityHandler, keepUnmatchedManager, mapToDatabaseTypeManager, databaseContext);
    }

    public async Task<TTarget> MapAsync<TSource, TTarget>(TSource source, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer)
        where TSource : class
        where TTarget : class
    {
        return await _toDatabaseRecursiveMapper.MapAsync(source, includer, _context);
    }
}