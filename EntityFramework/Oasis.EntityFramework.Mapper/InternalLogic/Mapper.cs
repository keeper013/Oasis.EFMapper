namespace Oasis.EntityFramework.Mapper.InternalLogic;

using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using Oasis.EntityFramework.Mapper.Exceptions;

internal abstract class MapperSessionHandler : IMapperSessionHandler
{
    public MapperSessionHandler(IRecursiveMappingContext context)
    {
        Context = context;
    }

    public bool IsSessionOn => Context.ForceTrack;

    protected IRecursiveMappingContext Context { get; }

    public void StartSession()
    {
        if (Context.ForceTrack)
        {
            if (Context.HasTracked)
            {
                Context.Clear();
            }
        }
        else
        {
            Context.ForceTrack = true;
        }
    }

    public void StopSession()
    {
        if (Context.HasTracked)
        {
            Context.Clear();
        }

        Context.ForceTrack = false;
    }
}

internal sealed class ToDatabaseMapper : MapperSessionHandler, IToDatabaseMapper
{
    private readonly ToDatabaseRecursiveMapper _mapper;

    public ToDatabaseMapper(ToDatabaseRecursiveMapper mapper, IReadOnlyDictionary<Type, ITargetByIdTrackerFactory> targetByIdTrackerFactories, DbContext? databaseContext = null)
        : base(new RecursiveMappingContext(targetByIdTrackerFactories, mapper, databaseContext))
    {
        _mapper = mapper;
    }

    public DbContext DatabaseContext
    {
        set => Context.DatabaseContext = value;
    }

    public async Task<TTarget> MapAsync<TSource, TTarget>(TSource source, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer)
        where TSource : class
        where TTarget : class
    {
        if (Context.DatabaseContext == null)
        {
            throw new DbContextMissingException();
        }

        return await _mapper.MapAsync(source, includer, Context);
    }
}

internal sealed class ToMemoryMapper : MapperSessionHandler, IToMemoryMapper
{
    private readonly ToMemoryRecursiveMapper _mapper;

    public ToMemoryMapper(ToMemoryRecursiveMapper mapper, IReadOnlyDictionary<Type, ITargetByIdTrackerFactory> targetByIdTrackerFactories)
        : base(new RecursiveMappingContext(targetByIdTrackerFactories, mapper))
    {
        _mapper = mapper;
    }

    public TTarget Map<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class
        => _mapper.MapNew<TSource, TTarget>(source, Context);
}

internal sealed class Mapper : MapperSessionHandler, IMapper
{
    private readonly ToDatabaseRecursiveMapper _toDatabaseMapper;
    private readonly ToMemoryRecursiveMapper _toMemoryMapper;

    public Mapper(
        ToDatabaseRecursiveMapper toDatabaseMapper,
        ToMemoryRecursiveMapper toMemoryMapper,
        IReadOnlyDictionary<Type, ITargetByIdTrackerFactory> targetByIdTrackerFactories,
        DbContext? databaseContext = null)
        : base(new RecursiveMappingContext(targetByIdTrackerFactories, toMemoryMapper, databaseContext))
    {
        _toDatabaseMapper = toDatabaseMapper;
        _toMemoryMapper = toMemoryMapper;
    }

    public DbContext DatabaseContext
    {
        set => Context.DatabaseContext = value;
    }

    public TTarget Map<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class
        => _toMemoryMapper.MapNew<TSource, TTarget>(source, Context);

    public async Task<TTarget> MapAsync<TSource, TTarget>(TSource source, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer)
        where TSource : class
        where TTarget : class
    {
        if (Context.DatabaseContext == null)
        {
            throw new DbContextMissingException();
        }

        return await _toDatabaseMapper.MapAsync(source, includer, Context);
    }
}

internal sealed class MapperFactory : IMapperFactory
{
    private readonly ToMemoryRecursiveMapper _toMemoryRecursiveMapper;
    private readonly ToDatabaseRecursiveMapper _toDatabaseRecursiveMapper;
    private readonly IReadOnlyDictionary<Type, ITargetByIdTrackerFactory> _targetByIdTrackerFactories;

    public MapperFactory(
        KeepUnmatchedManager? keepUnmatchedManager,
        MapType defaultMapType,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapType>> mapTypeDictionary,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> toMemoryMappers,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> toDatabaseMappers,
        EntityTrackerData entityTrackerData,
        EntityHandlerData entityHandlerData)
    {
        _toMemoryRecursiveMapper = new ToMemoryRecursiveMapper(toMemoryMappers, entityTrackerData, entityHandlerData);
        _toDatabaseRecursiveMapper = new ToDatabaseRecursiveMapper(keepUnmatchedManager, defaultMapType, mapTypeDictionary, toDatabaseMappers, entityTrackerData, entityHandlerData);
        _targetByIdTrackerFactories = entityTrackerData.targetByIdTrackerFactories;
    }

    public IMapper MakeMapper(DbContext? databaseContext = null) => new Mapper(_toDatabaseRecursiveMapper, _toMemoryRecursiveMapper, _targetByIdTrackerFactories, databaseContext);

    public IToDatabaseMapper MakeToDatabaseMapper(DbContext? databaseContext = null)
        => new ToDatabaseMapper(_toDatabaseRecursiveMapper, _targetByIdTrackerFactories, databaseContext);

    public IToMemoryMapper MakeToMemoryMapper() => new ToMemoryMapper(_toMemoryRecursiveMapper, _targetByIdTrackerFactories);
}