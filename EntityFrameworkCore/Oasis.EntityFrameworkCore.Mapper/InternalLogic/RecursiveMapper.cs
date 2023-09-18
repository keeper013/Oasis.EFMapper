namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Exceptions;

internal abstract class RecursiveMapper
{
    private readonly IListTypeConstructor _listTypeConstructor;

    internal RecursiveMapper(IListTypeConstructor listTypeConstructor)
    {
        _listTypeConstructor = listTypeConstructor;
    }

    public TList ConstructListType<TList, TItem>()
        where TList : class, ICollection<TItem>
        where TItem : class
    {
        return _listTypeConstructor.Construct<TList, TItem>();
    }
}

internal sealed class ToDatabaseRecursiveMapper : RecursiveMapper, IRecursiveMapper<int>
{
    private readonly MapperSetLookUp _lookup;
    private readonly IScalarTypeConverter _scalarConverter;
    private readonly EntityHandler _entityHandler;
    private readonly KeepUnmatchedManager? _keepUnmatchedManager;
    private readonly MapToDatabaseTypeManager _mapToDatabaseTypeManager;

    public ToDatabaseRecursiveMapper(
        IScalarTypeConverter scalarConverter,
        IListTypeConstructor listTypeConstructor,
        MapperSetLookUp lookup,
        EntityHandler entityHandler,
        KeepUnmatchedManager? keepUnmatchedManager,
        MapToDatabaseTypeManager mapToDatabaseTypeManager)
        : base(listTypeConstructor)
    {
        _scalarConverter = scalarConverter;
        _lookup = lookup;
        _entityHandler = entityHandler;
        _keepUnmatchedManager = keepUnmatchedManager;
        _mapToDatabaseTypeManager = mapToDatabaseTypeManager;
        DatabaseContext = null!;
    }

    public DbContext DatabaseContext { private get; set; }

    public async Task<TTarget> MapAsync<TSource, TTarget>(
        TSource source,
        Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer,
        IEntityTracker<TTarget> tracker,
        IRecursiveMappingContext context)
        where TSource : class
        where TTarget : class
    {
        const string AsNoTrackingMethodCall = ".AsNoTracking";

        var sourceHasId = _entityHandler.HasId<TSource>() && !_entityHandler.IdIsEmpty(source);
        var mapType = _mapToDatabaseTypeManager.Get<TSource, TTarget>();

        if (sourceHasId)
        {
            TTarget? target;
            var identityEqualsExpression = _entityHandler.GetIdEqualsExpression<TSource, TTarget>(source);
            if (includer != default)
            {
                var includerString = includer.ToString();
                if (includerString!.Contains(AsNoTrackingMethodCall))
                {
                    throw new AsNoTrackingNotAllowedException(includerString);
                }

                target = await includer.Compile()(DatabaseContext.Set<TTarget>()).FirstOrDefaultAsync(identityEqualsExpression);
            }
            else
            {
                target = await DatabaseContext.Set<TTarget>().FirstOrDefaultAsync(identityEqualsExpression);
            }

            if (target != default)
            {
                if (!mapType.AllowsUpdate())
                {
                    throw new InsertToDatabaseWithExistingException();
                }
                else
                {
                    MapToExistingTarget(source, target, false, context, tracker!);
                    return target;
                }
            }
        }

        if (!mapType.AllowsInsert())
        {
            if (!sourceHasId)
            {
                throw new UpdateToDatabaseWithoutIdException();
            }
            else
            {
                throw new UpdateToDatabaseWithoutRecordException();
            }
        }

        return MapToNewTarget(source, sourceHasId, context, tracker!);
    }

    public TTarget? MapEntityProperty<TSource, TTarget>(TSource? source, TTarget? target, IRecursiveMappingContext context, string propertyName)
        where TSource : class
        where TTarget : class
    {
        if (source == default)
        {
            return default;
        }

        var trackedTarget = context.GetTracked<TSource, TTarget>(source, out var tracker);
        if (trackedTarget != null)
        {
            return trackedTarget;
        }

        var mapType = _mapToDatabaseTypeManager.Get<TSource, TTarget>();
        if (!_entityHandler.HasId<TSource>() || _entityHandler.IdIsEmpty(source))
        {
            if (!mapType.AllowsInsert())
            {
                throw new MapToDatabaseTypeException(typeof(TSource), typeof(TTarget), MapToDatabaseType.Insert);
            }

            return MapToNewTarget(source, false, context, tracker!);
        }

        if (target != default)
        {
            if (_entityHandler.IdEquals(source, target))
            {
                MapToExistingTarget(source, target, false, context, tracker!);
            }
            else
            {
                target = MapToExistingOrNewTarget(source, context, tracker!, mapType);
            }
        }
        else
        {
            target = MapToExistingOrNewTarget(source, context, tracker!, mapType);
        }

        return target;
    }

    public void MapListProperty<TSource, TTarget>(ICollection<TSource>? source, ICollection<TTarget> target, IRecursiveMappingContext context, string propertyName)
        where TSource : class
        where TTarget : class
    {
        var shadowSet = new HashSet<TTarget>(target);
        if (source != default)
        {
            var mapType = _mapToDatabaseTypeManager.Get<TSource, TTarget>();
            var sourceHasIdProperty = _entityHandler.HasId<TSource>();
            var unmatchedSources = new List<(TSource, IEntityTracker<TTarget>)>();
            foreach (var s in source)
            {
                if (s == default)
                {
                    continue;
                }

                var trackedTarget = context.GetTracked<TSource, TTarget>(s, out var tracker);
                if (trackedTarget != null)
                {
                    target.Add(trackedTarget);
                }
                else
                {
                    if (!sourceHasIdProperty || _entityHandler.IdIsEmpty(s))
                    {
                        if (!mapType.AllowsInsert())
                        {
                            throw new MapToDatabaseTypeException(typeof(TSource), typeof(TTarget), MapToDatabaseType.Insert);
                        }

                        target.Add(MapToNewTarget(s, false, context, tracker!));
                    }
                    else
                    {
                        var t = target.FirstOrDefault(i => _entityHandler.IdEquals(s, i));
                        if (t != default)
                        {
                            if (!mapType.AllowsUpdate())
                            {
                                throw new MapToDatabaseTypeException(typeof(TSource), typeof(TTarget), MapToDatabaseType.Update);
                            }

                            MapToExistingTarget(s, t, false, context, tracker!);

                            shadowSet.Remove(t);
                        }
                        else
                        {
                            unmatchedSources.Add((s, tracker!));
                        }
                    }
                }
            }

            if (unmatchedSources.Any())
            {
                var targetsFound = DatabaseContext.Set<TTarget>().Where(_entityHandler.GetContainsTargetIdExpression<TSource, TTarget>(unmatchedSources.Select(s => s.Item1).ToList())).ToList();
                if (targetsFound.Any() && !mapType.AllowsUpdate())
                {
                    throw new MapToDatabaseTypeException(typeof(TSource), typeof(TTarget), MapToDatabaseType.Update);
                }

                if (targetsFound.Count() != unmatchedSources.Count() && !mapType.AllowsInsert())
                {
                    throw new MapToDatabaseTypeException(typeof(TSource), typeof(TTarget), MapToDatabaseType.Insert);
                }

                foreach (var s in unmatchedSources)
                {
                    var t = targetsFound.Where(t => _entityHandler.IdEquals(s.Item1, t)).FirstOrDefault();
                    if (t != default)
                    {
                        MapToExistingTarget(s.Item1, t, false, context, s.Item2);
                    }
                    else
                    {
                        t = MapToNewTarget(s.Item1, true, context, s.Item2);
                    }

                    target.Add(t);
                }
            }
        }

        if (shadowSet.Any() && (_keepUnmatchedManager == null || !_keepUnmatchedManager.KeepUnmatched(propertyName)))
        {
            foreach (var toBeRemoved in shadowSet)
            {
                target.Remove(toBeRemoved);
            }
        }
    }

    private void Map<TSource, TTarget>(TSource source, TTarget target, bool mapId, IRecursiveMappingContext context)
        where TSource : class
        where TTarget : class
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);

        var mapperSet = _lookup.LookUp(sourceType, targetType);
        if (mapperSet.HasValue)
        {
            var mapper = mapperSet.Value;
            try
            {
                _keepUnmatchedManager?.Push(sourceType, targetType);
                (mapper.customPropertiesMapper as Action<TSource, TTarget>)?.Invoke(source, target);

                if (mapId)
                {
                    (mapper.keyMapper as Utilities.MapKeyProperties<TSource, TTarget>)?.Invoke(source, target, _scalarConverter, true);
                }

                (mapper.contentMapper as Utilities.MapProperties<TSource, TTarget, int>)?.Invoke(source, target, _scalarConverter, this, context);
            }
            finally
            {
                _keepUnmatchedManager?.Pop();
            }
        }
        else
        {
            throw new UnregisteredMappingException(sourceType, targetType);
        }
    }

    private TTarget MapToNewTarget<TSource, TTarget>(TSource source, bool mapId, IRecursiveMappingContext context, IEntityTracker<TTarget> tracker)
        where TSource : class
        where TTarget : class
    {
        var newTarget = _entityHandler.Make<TTarget>();
        tracker.Track(newTarget);
        Map(source, newTarget, mapId, context);
        DatabaseContext.Set<TTarget>().Add(newTarget);
        return newTarget;
    }

    private void MapToExistingTarget<TSource, TTarget>(TSource source, TTarget existingTarget, bool mapId, IRecursiveMappingContext context, IEntityTracker<TTarget> tracker)
        where TSource : class
        where TTarget : class
    {
        if (_entityHandler.HasConcurrencyToken<TSource>() && _entityHandler.HasConcurrencyToken<TTarget>()
            && !_entityHandler.ConcurrencyTokenIsEmpty(source) && !_entityHandler.ConcurrencyTokenIsEmpty(existingTarget)
            && !_entityHandler.ConcurrencyTokenEquals(source, existingTarget))
        {
            throw new ConcurrencyTokenException(typeof(TSource), typeof(TTarget));
        }

        tracker.Track(existingTarget);
        Map(source, existingTarget, mapId, context);
    }

    private TTarget MapToExistingOrNewTarget<TSource, TTarget>(TSource source, IRecursiveMappingContext context, IEntityTracker<TTarget> tracker, MapToDatabaseType mapType)
        where TSource : class
        where TTarget : class
    {
        var identityEqualsExpression = _entityHandler.GetIdEqualsExpression<TSource, TTarget>(source);
        var target = DatabaseContext.Set<TTarget>().FirstOrDefault(identityEqualsExpression);
        if (target == null)
        {
            if (!mapType.AllowsInsert())
            {
                throw new MapToDatabaseTypeException(typeof(TSource), typeof(TTarget), MapToDatabaseType.Insert);
            }

            target = _entityHandler.Make<TTarget>();
            DatabaseContext.Set<TTarget>().Add(target);
        }
        else
        {
            if (!mapType.AllowsUpdate())
            {
                throw new MapToDatabaseTypeException(typeof(TSource), typeof(TTarget), MapToDatabaseType.Update);
            }

            if (_entityHandler.HasConcurrencyToken<TSource>() && _entityHandler.HasConcurrencyToken<TTarget>()
                && !_entityHandler.ConcurrencyTokenIsEmpty(source) && !_entityHandler.ConcurrencyTokenIsEmpty(target)
                && !_entityHandler.ConcurrencyTokenEquals(source, target))
            {
                throw new ConcurrencyTokenException(typeof(TSource), typeof(TTarget));
            }
        }

        tracker.Track(target);
        Map(source, target, true, context);
        return target;
    }
}

internal sealed class ToMemoryRecursiveMapper : RecursiveMapper, IRecursiveMapper<int>
{
    private readonly MapperSetLookUp _lookup;
    private readonly IScalarTypeConverter _scalarConverter;
    private readonly EntityHandler _entityHandler;

    public ToMemoryRecursiveMapper(
        IScalarTypeConverter scalarConverter,
        IListTypeConstructor listTypeConstructor,
        MapperSetLookUp lookup,
        EntityHandler entityHandler)
        : base(listTypeConstructor)
    {
        _scalarConverter = scalarConverter;
        _lookup = lookup;
        _entityHandler = entityHandler;
    }

    public TTarget? MapEntityProperty<TSource, TTarget>(TSource? source, TTarget? target, IRecursiveMappingContext context, string propertyName)
        where TSource : class
        where TTarget : class
    {
        if (source != default)
        {
            return context.TrackIfNecessaryAndMap(source, target, () => _entityHandler.Make<TTarget>(), (s, t, c) => Map(s, t, c), true);
        }

        return default;
    }

    public void MapListProperty<TSource, TTarget>(ICollection<TSource>? source, ICollection<TTarget> target, IRecursiveMappingContext context, string propertyName)
        where TSource : class
        where TTarget : class
    {
        if (source != default)
        {
            var cach = new Dictionary<int, TTarget>();
            foreach (var s in source)
            {
                var sourceHashCode = s.GetHashCode();
                if (!cach.TryGetValue(sourceHashCode, out var t))
                {
                    t = context.TrackIfNecessaryAndMap(s, null, () => _entityHandler.Make<TTarget>(), (s, t, c) => Map(s, t, c), true);
                    cach[sourceHashCode] = t;
                }

                target.Add(t);
            }
        }
    }

    internal void Map<TSource, TTarget>(TSource source, TTarget target, IRecursiveMappingContext context)
        where TSource : class
        where TTarget : class
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);

        var mapperSet = _lookup.LookUp(sourceType, targetType);
        if (mapperSet.HasValue)
        {
            var mapper = mapperSet.Value;
            (mapper.customPropertiesMapper as Action<TSource, TTarget>)?.Invoke(source, target);
            (mapper.keyMapper as Utilities.MapKeyProperties<TSource, TTarget>)?.Invoke(source, target, _scalarConverter, false);
            (mapper.contentMapper as Utilities.MapProperties<TSource, TTarget, int>)?.Invoke(source, target, _scalarConverter, this, context);
        }
        else
        {
            throw new UnregisteredMappingException(sourceType, targetType);
        }
    }
}