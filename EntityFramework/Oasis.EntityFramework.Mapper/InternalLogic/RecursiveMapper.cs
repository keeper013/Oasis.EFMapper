namespace Oasis.EntityFramework.Mapper.InternalLogic;

using System;
using System.Data.Entity;
using System.Linq.Expressions;
using Oasis.EntityFramework.Mapper.Exceptions;

internal abstract class RecursiveMapper : IRecursiveMapper<int>
{
    private readonly MapperSetLookUp _lookup;
    private readonly IScalarTypeConverter _scalarConverter;
    private readonly IListTypeConstructor _listTypeConstructor;

    internal RecursiveMapper(
        IScalarTypeConverter scalarConverter,
        IListTypeConstructor listTypeConstructor,
        MapperSetLookUp lookup,
        EntityHandler entityHandler)
    {
        _scalarConverter = scalarConverter;
        _listTypeConstructor = listTypeConstructor;
        _lookup = lookup;
        EntityHandler = entityHandler;
    }

    protected EntityHandler EntityHandler { get; }

    public abstract TTarget? MapEntityProperty<TSource, TTarget>(TSource? source, TTarget? target, IRecursiveMappingContext context, string propertyName)
        where TSource : class
        where TTarget : class;

    public abstract void MapListProperty<TSource, TTarget>(ICollection<TSource>? source, ICollection<TTarget> target, IRecursiveMappingContext context, string propertyName)
        where TSource : class
        where TTarget : class;

    public TList ConstructListType<TList, TItem>()
        where TList : class, ICollection<TItem>
        where TItem : class
    {
        return _listTypeConstructor.Construct<TList, TItem>();
    }

    internal void Map<TSource, TTarget>(TSource source, TTarget target, MapKeyProperties mapKeyProperties, IRecursiveMappingContext context)
        where TSource : class
        where TTarget : class
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);

        var mapperSet = _lookup.LookUp(sourceType, targetType);
        if (mapperSet.HasValue)
        {
            var mapper = mapperSet.Value;
            using var ctx = new RecursiveContextPopper(context, sourceType, targetType);
            (mapper.customPropertiesMapper as Action<TSource, TTarget>)?.Invoke(source, target);

            if (mapKeyProperties != MapKeyProperties.None)
            {
                (mapper.keyMapper as Utilities.MapKeyProperties<TSource, TTarget>)?.Invoke(source, target, _scalarConverter, mapKeyProperties == MapKeyProperties.IdOnly);
            }

            (mapper.contentMapper as Utilities.MapProperties<TSource, TTarget, int>)?.Invoke(source, target, _scalarConverter, this, context);
        }
    }
}

internal sealed class ToDatabaseRecursiveMapper : RecursiveMapper
{
    private readonly KeepUnmatchedManager _keepUnmatchedManager;
    private readonly MapToDatabaseTypeManager _mapToDatabaseTypeManager;
    private readonly DbContext _databaseContext;

    public ToDatabaseRecursiveMapper(
        IScalarTypeConverter scalarConverter,
        IListTypeConstructor listTypeConstructor,
        MapperSetLookUp lookup,
        EntityHandler entityHandler,
        KeepUnmatchedManager keepUnmatchedManager,
        MapToDatabaseTypeManager mapToDatabaseTypeManager,
        DbContext databaseContext)
        : base(scalarConverter, listTypeConstructor, lookup, entityHandler)
    {
        _keepUnmatchedManager = keepUnmatchedManager;
        _mapToDatabaseTypeManager = mapToDatabaseTypeManager;
        _databaseContext = databaseContext;
    }

    public async Task<TTarget> MapAsync<TSource, TTarget>(
        TSource source,
        Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer,
        IRecursiveMappingContext context)
        where TSource : class
        where TTarget : class
    {
        const string AsNoTrackingMethodCall = ".AsNoTracking";

        var sourceHasId = EntityHandler.HasId<TSource>() && !EntityHandler.IdIsEmpty(source);
        var mapType = _mapToDatabaseTypeManager.Get<TSource, TTarget>();
        var trackedTarget = context.GetTracked<TSource, TTarget>(source, out var tracker);
        if (trackedTarget != null)
        {
            return trackedTarget;
        }

        if (sourceHasId)
        {
            TTarget? target;
            var identityEqualsExpression = EntityHandler.GetIdEqualsExpression<TSource, TTarget>(source);
            if (includer != default)
            {
                var includerString = includer.ToString();
                if (includerString.Contains(AsNoTrackingMethodCall))
                {
                    throw new AsNoTrackingNotAllowedException(includerString);
                }

                target = await includer.Compile()(_databaseContext.Set<TTarget>()).FirstOrDefaultAsync(identityEqualsExpression);
            }
            else
            {
                target = await _databaseContext.Set<TTarget>().FirstOrDefaultAsync(identityEqualsExpression);
            }

            if (target != default)
            {
                if (!mapType.AllowsUpdate())
                {
                    throw new InsertToDatabaseWithExistingException();
                }
                else
                {
                    MapToExistingTarget(source, target, MapKeyProperties.None, context, tracker!);
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

        return MapToNewTarget(source, sourceHasId ? MapKeyProperties.IdOnly : MapKeyProperties.None, context, tracker!);
    }

    public override TTarget? MapEntityProperty<TSource, TTarget>(TSource? source, TTarget? target, IRecursiveMappingContext context, string propertyName)
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
        if (!EntityHandler.HasId<TSource>() || EntityHandler.IdIsEmpty(source))
        {
            if (!mapType.AllowsInsert())
            {
                throw new MapToDatabaseTypeException(typeof(TSource), typeof(TTarget), MapToDatabaseType.Insert);
            }

            return MapToNewTarget(source, MapKeyProperties.None, context, tracker!);
        }

        if (target != default)
        {
            if (EntityHandler.IdEquals(source, target))
            {
                MapToExistingTarget(source, target, MapKeyProperties.None, context, tracker!);
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

    public override void MapListProperty<TSource, TTarget>(ICollection<TSource>? source, ICollection<TTarget> target, IRecursiveMappingContext context, string propertyName)
        where TSource : class
        where TTarget : class
    {
        var shadowSet = new HashSet<TTarget>(target);
        if (source != default)
        {
            var mapType = _mapToDatabaseTypeManager.Get<TSource, TTarget>();
            var sourceHasIdProperty = EntityHandler.HasId<TSource>();
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
                    if (!sourceHasIdProperty || EntityHandler.IdIsEmpty(s))
                    {
                        if (!mapType.AllowsInsert())
                        {
                            throw new MapToDatabaseTypeException(typeof(TSource), typeof(TTarget), MapToDatabaseType.Insert);
                        }

                        target.Add(MapToNewTarget<TSource, TTarget>(s, MapKeyProperties.None, context, tracker!));
                    }
                    else
                    {
                        var t = target.FirstOrDefault(i => EntityHandler.IdEquals(s, i));
                        if (t != default)
                        {
                            if (!mapType.AllowsUpdate())
                            {
                                throw new MapToDatabaseTypeException(typeof(TSource), typeof(TTarget), MapToDatabaseType.Update);
                            }

                            MapToExistingTarget(s, t, MapKeyProperties.None, context, tracker!);

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
                var targetsFound = _databaseContext.Set<TTarget>().Where(EntityHandler.GetContainsTargetIdExpression<TSource, TTarget>(unmatchedSources.Select(s => s.Item1).ToList())).ToList();
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
                    var t = targetsFound.Where(t => EntityHandler.IdEquals(s.Item1, t)).FirstOrDefault();
                    if (t != default)
                    {
                        MapToExistingTarget(s.Item1, t, MapKeyProperties.None, context, s.Item2);
                    }
                    else
                    {
                        t = MapToNewTarget(s.Item1, MapKeyProperties.IdOnly, context, s.Item2);
                    }

                    target.Add(t);
                }
            }
        }

        var current = context.Current;
        if (shadowSet.Any() && !_keepUnmatchedManager.KeepUnmatched(current.Item1, current.Item2, propertyName))
        {
            foreach (var toBeRemoved in shadowSet)
            {
                target.Remove(toBeRemoved);
            }
        }
    }

    private TTarget MapToNewTarget<TSource, TTarget>(TSource source, MapKeyProperties mapKeyProperties, IRecursiveMappingContext context, IEntityTracker<TTarget> tracker)
        where TSource : class
        where TTarget : class
    {
        var newTarget = EntityHandler.Make<TTarget>();
        tracker.Track(newTarget);
        Map(source, newTarget, mapKeyProperties, context);
        _databaseContext.Set<TTarget>().Add(newTarget);
        return newTarget;
    }

    private void MapToExistingTarget<TSource, TTarget>(TSource source, TTarget existingTarget, MapKeyProperties mapKeyProperties, IRecursiveMappingContext context, IEntityTracker<TTarget> tracker)
        where TSource : class
        where TTarget : class
    {
        if (EntityHandler.HasConcurrencyToken<TSource>() && EntityHandler.HasConcurrencyToken<TTarget>()
            && !EntityHandler.ConcurrencyTokenIsEmpty(source) && !EntityHandler.ConcurrencyTokenIsEmpty(existingTarget)
            && !EntityHandler.ConcurrencyTokenEquals(source, existingTarget))
        {
            throw new ConcurrencyTokenException(typeof(TSource), typeof(TTarget));
        }

        tracker.Track(existingTarget);
        Map(source, existingTarget, mapKeyProperties, context);
    }

    private TTarget MapToExistingOrNewTarget<TSource, TTarget>(TSource source, IRecursiveMappingContext context, IEntityTracker<TTarget> tracker, MapToDatabaseType mapType)
        where TSource : class
        where TTarget : class
    {
        var identityEqualsExpression = EntityHandler.GetIdEqualsExpression<TSource, TTarget>(source);
        var target = _databaseContext.Set<TTarget>().FirstOrDefault(identityEqualsExpression);
        if (target == null)
        {
            if (!mapType.AllowsInsert())
            {
                throw new MapToDatabaseTypeException(typeof(TSource), typeof(TTarget), MapToDatabaseType.Insert);
            }

            target = EntityHandler.Make<TTarget>();
            _databaseContext.Set<TTarget>().Add(target);
        }
        else
        {
            if (!mapType.AllowsUpdate())
            {
                throw new MapToDatabaseTypeException(typeof(TSource), typeof(TTarget), MapToDatabaseType.Update);
            }

            if (EntityHandler.HasConcurrencyToken<TSource>() && EntityHandler.HasConcurrencyToken<TTarget>()
                && !EntityHandler.ConcurrencyTokenIsEmpty(source) && !EntityHandler.ConcurrencyTokenIsEmpty(target)
                && !EntityHandler.ConcurrencyTokenEquals(source, target))
            {
                throw new ConcurrencyTokenException(typeof(TSource), typeof(TTarget));
            }
        }

        tracker.Track(target);
        Map(source, target, MapKeyProperties.IdOnly, context);
        return target;
    }
}

internal sealed class ToMemoryRecursiveMapper : RecursiveMapper
{
    public ToMemoryRecursiveMapper(
        IScalarTypeConverter scalarConverter,
        IListTypeConstructor listTypeConstructor,
        MapperSetLookUp lookup,
        EntityHandler entityHandler)
        : base(scalarConverter, listTypeConstructor, lookup, entityHandler)
    {
    }

    public override TTarget? MapEntityProperty<TSource, TTarget>(TSource? source, TTarget? target, IRecursiveMappingContext context, string propertyName)
        where TSource : class
        where TTarget : class
    {
        if (source != default)
        {
            var trackedTarget = context.GetTracked<TSource, TTarget>(source, out var tracker);
            if (trackedTarget != null)
            {
                return trackedTarget;
            }
            else
            {
                if (target == default)
                {
                    target = EntityHandler.Make<TTarget>();
                }

                tracker!.Track(target);
                Map(source, target!, MapKeyProperties.IdAndConcurrencyToken, context);
                return target;
            }
        }

        return default;
    }

    public override void MapListProperty<TSource, TTarget>(ICollection<TSource>? source, ICollection<TTarget> target, IRecursiveMappingContext context, string propertyName)
    {
        if (source != default)
        {
            foreach (var s in source)
            {
                var t = context.GetTracked<TSource, TTarget>(s, out var tracker);
                if (t == default)
                {
                    t = EntityHandler.Make<TTarget>();
                    tracker!.Track(t);
                    Map(s, t, MapKeyProperties.IdAndConcurrencyToken, context);
                }

                target.Add(t);
            }
        }
    }
}