﻿namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Exceptions;

internal record struct EntityHandlerData(
    IReadOnlyDictionary<Type, TypeKeyProxy> entityIdProxies,
    IReadOnlyDictionary<Type, TypeKeyProxy> entityConcurrencyTokenProxies,
    IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> entityIdComparers,
    IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> entityConcurrencyTokenComparers,
    IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> sourceIdForTarget,
    IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> sourceIdListContainsTargetId,
    IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> scalarTypeConverters,
    IReadOnlyDictionary<Type, Delegate> factoryMethods,
    Dictionary<Type, Delegate> listTypeFactoryMethods);

internal abstract class RecursiveMapperBase
{
    private static readonly Type[] ListTypes = new[] { typeof(ICollection<>), typeof(IList<>), typeof(List<>) };

    private readonly IReadOnlyDictionary<Type, TypeKeyProxy> _entityIdProxies;
    private readonly IReadOnlyDictionary<Type, TypeKeyProxy> _entityConcurrencyTokenProxies;
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> _entityIdComparers;
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> _entityConcurrencyTokenComparers;
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> _targetIdEqualsSourceId;
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> _sourceIdListContainsTargetId;
    private readonly IReadOnlyDictionary<Type, Delegate> _factoryMethods;
    private readonly Dictionary<Type, Delegate> _listTypeFactoryMethods;

    internal RecursiveMapperBase(EntityHandlerData entityHandlerData)
    {
        _entityIdProxies = entityHandlerData.entityIdProxies;
        _entityConcurrencyTokenProxies = entityHandlerData.entityConcurrencyTokenProxies;
        _entityIdComparers = entityHandlerData.entityIdComparers;
        _entityConcurrencyTokenComparers = entityHandlerData.entityConcurrencyTokenComparers;
        _targetIdEqualsSourceId = entityHandlerData.sourceIdForTarget;
        _sourceIdListContainsTargetId = entityHandlerData.sourceIdListContainsTargetId;
        ScalarTypeConverters = entityHandlerData.scalarTypeConverters;
        _factoryMethods = entityHandlerData.factoryMethods;
        _listTypeFactoryMethods = entityHandlerData.listTypeFactoryMethods;
    }

    protected IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> ScalarTypeConverters { get; init; }

    public TList ConstructListType<TList, TItem>()
        where TList : class, ICollection<TItem>
        where TItem : class
    {
        if (_listTypeFactoryMethods.TryGetValue(typeof(TList), out var @delegate))
        {
            return ((Func<TList>)@delegate)();
        }

        var listType = typeof(TList);
        if (listType.IsGenericType)
        {
            if (ListTypes.Contains(listType.GetGenericTypeDefinition()))
            {
                _listTypeFactoryMethods.Add(listType, CreateList<TList, TItem>);
                return CreateList<TList, TItem>();
            }
        }

        throw new MissingFactoryMethodException(typeof(TList));
    }

    protected bool HasId<TEntity>()
        where TEntity : class
        => _entityIdProxies.ContainsKey(typeof(TEntity));

    protected bool HasConcurrencyToken<TEntity>()
        where TEntity : class
        => _entityConcurrencyTokenProxies.ContainsKey(typeof(TEntity));

    protected bool IdIsEmpty<TEntity>(TEntity entity)
        where TEntity : class
        => ((Utilities.ScalarPropertyIsEmpty<TEntity>)_entityIdProxies[typeof(TEntity)].isEmpty)(entity);

    protected bool ConcurrencyTokenIsEmpty<TEntity>(TEntity entity)
        where TEntity : class
        => ((Utilities.ScalarPropertyIsEmpty<TEntity>)_entityConcurrencyTokenProxies[typeof(TEntity)].isEmpty)(entity);

    protected bool IdEquals<TSource, TTarget>(TSource source, TTarget target)
        where TSource : class
        where TTarget : class
        => ((Utilities.ScalarPropertiesAreEqual<TSource, TTarget>)_entityIdComparers[typeof(TSource)][typeof(TTarget)])(source, target, ScalarTypeConverters);

    protected bool ConcurrencyTokenEquals<TSource, TTarget>(TSource source, TTarget target)
        where TSource : class
        where TTarget : class
        => ((Utilities.ScalarPropertiesAreEqual<TSource, TTarget>)_entityConcurrencyTokenComparers[typeof(TSource)][typeof(TTarget)])(source, target, ScalarTypeConverters);

    protected PropertyInfo GetIdProperty<TEntity>()
        where TEntity : class
        => _entityIdProxies[typeof(TEntity)].property;

    protected Expression<Func<TTarget, bool>> GetContainsTargetIdExpression<TSource, TTarget>(List<TSource> sourceList)
        where TSource : class
        where TTarget : class
        => ((Utilities.GetSourceIdListContainsTargetId<TSource, TTarget>)_sourceIdListContainsTargetId.Find(typeof(TSource), typeof(TTarget))!)(sourceList, ScalarTypeConverters);

    protected Expression<Func<TTarget, bool>> GetIdEqualsExpression<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class
        => ((Utilities.GetSourceIdEqualsTargetId<TSource, TTarget>)_targetIdEqualsSourceId.Find(typeof(TSource), typeof(TTarget))!)(source, ScalarTypeConverters);

    protected TEntity Make<TEntity>()
        where TEntity : class
        => ((Func<TEntity>)_factoryMethods[typeof(TEntity)])();

    private static TList CreateList<TList, TItem>()
        where TList : class, ICollection<TItem>
        where TItem : class
    {
        return (new List<TItem>() as TList)!;
    }
}

internal record struct EntityTrackerData(
    IReadOnlyDictionary<Type, Type> targetIdentityTypeMapping,
    IReadOnlyDictionary<Type, ITargetByIdTrackerFactory> targetByIdTrackerFactories,
    IReadOnlyDictionary<Type, IReadOnlySet<Type>>? loopDependencyMapping);

internal abstract class RecursiveMapperContext : RecursiveMapperBase, IRecursiveMapperContext
{
    private readonly IReadOnlyDictionary<Type, Type> _targetIdentityTypeMapping;
    private readonly IReadOnlyDictionary<Type, IReadOnlySet<Type>>? _loopDependencyMapping;

    protected RecursiveMapperContext(
        EntityTrackerData entityTrackerData,
        EntityHandlerData entityHandlerData)
        : base(entityHandlerData)
    {
        _targetIdentityTypeMapping = entityTrackerData.targetIdentityTypeMapping;
        _loopDependencyMapping = entityTrackerData.loopDependencyMapping;
    }

    public Type GetIdentityType(Type type) => _targetIdentityTypeMapping[type];

    public bool TargetIsIdentifyableById<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class
        => HasId<TSource>() && HasId<TTarget>() && !IdIsEmpty(source);

    protected bool HasLoopDependency<TSource, TTarget>()
        where TSource : class
        where TTarget : class
        => _loopDependencyMapping != null && _loopDependencyMapping.Contains(typeof(TSource), typeof(TTarget));
}

internal sealed class ToDatabaseRecursiveMapper : RecursiveMapperContext, IRecursiveMapper<int>
{
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> _mappers;
    private readonly KeepUnmatchedManager? _keepUnmatchedManager;
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapType>> _mapTypeDictionary;
    private readonly MapType _defaultMapType;

    public ToDatabaseRecursiveMapper(
        KeepUnmatchedManager? keepUnmatchedManager,
        MapType defaultMapType,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapType>> mapTypeDictionary,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> mappers,
        EntityTrackerData entityTrackerData,
        EntityHandlerData entityHandlerData)
        : base(entityTrackerData, entityHandlerData)
    {
        _mappers = mappers;
        _keepUnmatchedManager = keepUnmatchedManager;
        _defaultMapType = defaultMapType;
        _mapTypeDictionary = mapTypeDictionary;
    }

    public async Task<TTarget> MapAsync<TSource, TTarget>(
        TSource source,
        Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer,
        IRecursiveMappingContext context)
        where TSource : class
        where TTarget : class
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);
        if (_mappers.Find(sourceType, targetType) is not Utilities.MapToDatabase<TSource, TTarget, int> mapper)
        {
            throw new UnregisteredMappingException(sourceType, targetType);
        }

        const string AsNoTrackingMethodCall = ".AsNoTracking";
        IEntityTracker<TTarget>? tracker = null;
        if (context.ForceTrack)
        {
            var target = context.GetTracked(source, out tracker);
            if (target != default)
            {
                return target;
            }
        }
        else if (HasLoopDependency<TSource, TTarget>())
        {
            tracker = context.GetTracker<TSource, TTarget>(source);
        }

        var sourceHasId = HasId<TSource>() && !IdIsEmpty(source);
        var mapType = _mapTypeDictionary.DefaultIfNotFound(sourceType, typeof(TTarget), _defaultMapType);

        if (sourceHasId)
        {
            TTarget? target;
            var identityEqualsExpression = GetIdEqualsExpression<TSource, TTarget>(source);
            if (includer != default)
            {
                var includerString = includer.ToString();
                if (includerString!.Contains(AsNoTrackingMethodCall))
                {
                    throw new AsNoTrackingNotAllowedException(includerString);
                }

                target = await includer.Compile()(context.DatabaseContext!.Set<TTarget>()).FirstOrDefaultAsync(identityEqualsExpression);
            }
            else
            {
                target = await context.DatabaseContext!.Set<TTarget>().FirstOrDefaultAsync(identityEqualsExpression);
            }

            if (target != default)
            {
                if (!mapType.AllowsUpdate())
                {
                    throw new InsertToDatabaseWithExistingException();
                }
                else
                {
                    MapToExistingTarget(source, target, mapper, context, tracker);
                    if (context.HasTracked && !context.ForceTrack)
                    {
                        context.Clear();
                    }

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

        var t = MapToNewTarget(source, mapper, context, sourceHasId, tracker);
        if (context.HasTracked && !context.ForceTrack)
        {
            context.Clear();
        }

        return t;
    }

    public TTarget? MapEntityProperty<TSource, TTarget>(TSource? source, TTarget? target, IRecursiveMappingContext context)
        where TSource : class
        where TTarget : class
    {
        if (source == default)
        {
            return default;
        }

        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);
        if (_mappers.Find(sourceType, targetType) is not Utilities.MapToDatabase<TSource, TTarget, int> mapper)
        {
            throw new UnregisteredMappingException(sourceType, targetType);
        }

        IEntityTracker<TTarget>? tracker = null;
        if (context.ForceTrack || HasLoopDependency<TSource, TTarget>())
        {
            var trackedTarget = context.GetTracked(source, out tracker);
            if (trackedTarget != null)
            {
                return trackedTarget;
            }
        }

        var mapType = _mapTypeDictionary.DefaultIfNotFound(sourceType, targetType, _defaultMapType);
        if (!HasId<TSource>() || IdIsEmpty(source))
        {
            if (!mapType.AllowsInsert())
            {
                throw new MapTypeException(sourceType, targetType, MapType.Insert);
            }

            return MapToNewTarget(source, mapper, context, false, tracker);
        }

        if (target != default)
        {
            if (IdEquals(source, target))
            {
                MapToExistingTarget(source, target, mapper, context, tracker);
            }
            else
            {
                target = MapToExistingOrNewTarget(source, context, mapper, tracker, mapType);
            }
        }
        else
        {
            target = MapToExistingOrNewTarget(source, context, mapper, tracker, mapType);
        }

        return target;
    }

    public void MapListProperty<TSource, TTarget>(ICollection<TSource>? source, ICollection<TTarget> target, string propertyName, IRecursiveMappingContext context)
        where TSource : class
        where TTarget : class
    {
        if (source != default)
        {
            var sourceType = typeof(TSource);
            var targetType = typeof(TTarget);
            if (_mappers.Find(sourceType, targetType) is not Utilities.MapToDatabase<TSource, TTarget, int> mapper)
            {
                throw new UnregisteredMappingException(sourceType, targetType);
            }

            var shadowSet = new HashSet<TTarget>(target);
            var mapType = _mapTypeDictionary.DefaultIfNotFound(sourceType, targetType, _defaultMapType);
            var sourceHasIdProperty = HasId<TSource>();
            var unmatchedSources = new List<(TSource, IEntityTracker<TTarget>?)>();
            var needToTrack = context.ForceTrack || HasLoopDependency<TSource, TTarget>();
            IEntityTracker<TTarget>? tracker = null;
            foreach (var s in source)
            {
                if (s == default)
                {
                    continue;
                }

                if (needToTrack)
                {
                    var trackedTarget = context.GetTracked(s, out tracker);
                    if (trackedTarget != null)
                    {
                        target.Add(trackedTarget);
                        continue;
                    }
                }

                if (!sourceHasIdProperty || IdIsEmpty(s))
                {
                    if (!mapType.AllowsInsert())
                    {
                        throw new MapTypeException(sourceType, targetType, MapType.Insert);
                    }

                    target.Add(MapToNewTarget(s, mapper, context, false, tracker));
                }
                else
                {
                    var t = target.FirstOrDefault(i => IdEquals(s, i));
                    if (t != default)
                    {
                        if (!mapType.AllowsUpdate())
                        {
                            throw new MapTypeException(sourceType, targetType, MapType.Update);
                        }

                        MapToExistingTarget(s, t, mapper, context, tracker);

                        shadowSet.Remove(t);
                    }
                    else
                    {
                        unmatchedSources.Add((s, tracker));
                    }
                }
            }

            if (unmatchedSources.Any())
            {
                var targetsFound = context.DatabaseContext!.Set<TTarget>().Where(GetContainsTargetIdExpression<TSource, TTarget>(unmatchedSources.Select(s => s.Item1).ToList())).ToList();
                if (targetsFound.Any() && !mapType.AllowsUpdate())
                {
                    throw new MapTypeException(sourceType, targetType, MapType.Update);
                }

                if (targetsFound.Count != unmatchedSources.Count && !mapType.AllowsInsert())
                {
                    throw new MapTypeException(sourceType, targetType, MapType.Insert);
                }

                foreach (var s in unmatchedSources)
                {
                    var t = targetsFound.Where(t => IdEquals(s.Item1, t)).FirstOrDefault();
                    if (t != default)
                    {
                        MapToExistingTarget(s.Item1, t, mapper, context, s.Item2);
                    }
                    else
                    {
                        t = MapToNewTarget(s.Item1, mapper, context, true, s.Item2);
                    }

                    target.Add(t);
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
    }

    private void DoMapping<TSource, TTarget>(TSource source, TTarget target, Utilities.MapToDatabase<TSource, TTarget, int> mapper, IRecursiveMappingContext context, bool mapId)
        where TSource : class
        where TTarget : class
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);

        try
        {
            _keepUnmatchedManager?.Push(sourceType, targetType);
            mapper.Invoke(source, target, ScalarTypeConverters, this, context, mapId);
        }
        finally
        {
            _keepUnmatchedManager?.Pop();
        }
    }

    private TTarget MapToNewTarget<TSource, TTarget>(TSource source, Utilities.MapToDatabase<TSource, TTarget, int> mapper, IRecursiveMappingContext context, bool mapId, IEntityTracker<TTarget>? tracker)
        where TSource : class
        where TTarget : class
    {
        var newTarget = Make<TTarget>();
        tracker?.Track(newTarget);
        DoMapping(source, newTarget, mapper, context, mapId);
        context.DatabaseContext!.Set<TTarget>().Add(newTarget);
        return newTarget;
    }

    private void MapToExistingTarget<TSource, TTarget>(TSource source, TTarget existingTarget, Utilities.MapToDatabase<TSource, TTarget, int> mapper, IRecursiveMappingContext context, IEntityTracker<TTarget>? tracker)
        where TSource : class
        where TTarget : class
    {
        if (HasConcurrencyToken<TSource>() && HasConcurrencyToken<TTarget>()
            && !ConcurrencyTokenIsEmpty(source) && !ConcurrencyTokenIsEmpty(existingTarget)
            && !ConcurrencyTokenEquals(source, existingTarget))
        {
            throw new ConcurrencyTokenException(typeof(TSource), typeof(TTarget));
        }

        tracker?.Track(existingTarget);
        DoMapping(source, existingTarget, mapper, context, false);
    }

    private TTarget MapToExistingOrNewTarget<TSource, TTarget>(TSource source, IRecursiveMappingContext context, Utilities.MapToDatabase<TSource, TTarget, int> mapper, IEntityTracker<TTarget>? tracker, MapType mapType)
        where TSource : class
        where TTarget : class
    {
        var identityEqualsExpression = GetIdEqualsExpression<TSource, TTarget>(source);
        var target = context.DatabaseContext!.Set<TTarget>().FirstOrDefault(identityEqualsExpression);
        var isInsert = target == null;
        if (isInsert)
        {
            if (!mapType.AllowsInsert())
            {
                throw new MapTypeException(typeof(TSource), typeof(TTarget), MapType.Insert);
            }

            target = Make<TTarget>();
            context.DatabaseContext.Set<TTarget>().Add(target);
        }
        else
        {
            if (!mapType.AllowsUpdate())
            {
                throw new MapTypeException(typeof(TSource), typeof(TTarget), MapType.Update);
            }

            if (HasConcurrencyToken<TSource>() && HasConcurrencyToken<TTarget>()
                && !ConcurrencyTokenIsEmpty(source) && !ConcurrencyTokenIsEmpty(target!)
                && !ConcurrencyTokenEquals(source, target!))
            {
                throw new ConcurrencyTokenException(typeof(TSource), typeof(TTarget));
            }
        }

        tracker?.Track(target!);
        DoMapping(source, target!, mapper, context, isInsert);
        return target!;
    }
}

internal sealed class ToMemoryRecursiveMapper : RecursiveMapperContext, IRecursiveMapper<int>
{
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> _mappers;

    public ToMemoryRecursiveMapper(
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> mappers,
        EntityTrackerData entityTrackerData,
        EntityHandlerData entityHandlerData)
        : base(entityTrackerData, entityHandlerData)
    {
        _mappers = mappers;
    }

    public TTarget? MapEntityProperty<TSource, TTarget>(TSource? source, TTarget? target, IRecursiveMappingContext context)
        where TSource : class
        where TTarget : class
    {
        if (source != default)
        {
            if (_mappers.Find(typeof(TSource), typeof(TTarget)) is not Utilities.MapToMemory<TSource, TTarget, int> mapper)
            {
                throw new UnregisteredMappingException(typeof(TSource), typeof(TTarget));
            }

            IEntityTracker<TTarget>? tracker = null;
            if (context.ForceTrack || HasLoopDependency<TSource, TTarget>())
            {
                target = context.GetTracked(source, out tracker);
                if (target != default)
                {
                    return target;
                }
            }

            if (target == default)
            {
                target = Make<TTarget>();
            }

            tracker?.Track(target);

            mapper.Invoke(source, target, ScalarTypeConverters, this, context);
            return target;
        }

        return default;
    }

    public void MapListProperty<TSource, TTarget>(ICollection<TSource>? source, ICollection<TTarget> target, string propertyName, IRecursiveMappingContext context)
        where TSource : class
        where TTarget : class
    {
        if (source != default && source.Any())
        {
            if (_mappers.Find(typeof(TSource), typeof(TTarget)) is not Utilities.MapToMemory<TSource, TTarget, int> mapper)
            {
                throw new UnregisteredMappingException(typeof(TSource), typeof(TTarget));
            }

            var needToTrack = context.ForceTrack || HasLoopDependency<TSource, TTarget>();

            IEntityTracker<TTarget>? tracker = null;
            foreach (var s in source)
            {
                TTarget? t = null;
                if (needToTrack)
                {
                    t = context.GetTracked(s, out tracker);
                }

                if (t == default)
                {
                    t = Make<TTarget>();
                    tracker?.Track(t);
                    mapper.Invoke(s, t, ScalarTypeConverters, this, context);
                }

                target.Add(t);
            }
        }
    }

    internal TTarget MapNew<TSource, TTarget>(TSource source, TTarget? target, IRecursiveMappingContext context)
        where TSource : class
        where TTarget : class
    {
        if (_mappers.Find(typeof(TSource), typeof(TTarget)) is not Utilities.MapToMemory<TSource, TTarget, int> mapper)
        {
            throw new UnregisteredMappingException(typeof(TSource), typeof(TTarget));
        }

        IEntityTracker<TTarget>? tracker = null;
        if (context.ForceTrack)
        {
            var tracked = context.GetTracked(source, out tracker);
            if (tracked != default)
            {
                return tracked;
            }
        }
        else if (HasLoopDependency<TSource, TTarget>())
        {
            tracker = context.GetTracker<TSource, TTarget>(source);
        }

        target ??= Make<TTarget>();
        tracker?.Track(target);
        mapper.Invoke(source, target, ScalarTypeConverters, this, context);

        if (context.HasTracked && !context.ForceTrack)
        {
            context.Clear();
        }

        return target;
    }
}