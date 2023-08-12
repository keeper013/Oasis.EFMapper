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
        EntityBaseProxy entityBaseProxy)
    {
        _scalarConverter = scalarConverter;
        _listTypeConstructor = listTypeConstructor;
        _lookup = lookup;
        EntityBaseProxy = entityBaseProxy;
    }

    protected EntityBaseProxy EntityBaseProxy { get; }

    public abstract TTarget? MapEntityProperty<TSource, TTarget>(TSource? source, TTarget? target, IExistingTargetTracker existingTargetTracker, INewTargetTracker<int>? newTargetTracker, string propertyName)
        where TSource : class
        where TTarget : class;

    public abstract void MapListProperty<TSource, TTarget>(ICollection<TSource>? source, ICollection<TTarget> target, IExistingTargetTracker existingTargetTracker, INewTargetTracker<int>? newTargetTracker, string propertyName)
        where TSource : class
        where TTarget : class;

    public TList ConstructListType<TList, TItem>()
        where TList : class, ICollection<TItem>
        where TItem : class
    {
        return _listTypeConstructor.Construct<TList, TItem>();
    }

    internal void Map<TSource, TTarget>(TSource source, TTarget target, MapKeyProperties mapKeyProperties, IExistingTargetTracker? existingTargetTracker, INewTargetTracker<int>? newTargetTracker, MappingToDatabaseContext? context = null)
        where TSource : class
        where TTarget : class
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);

        if (EntityBaseProxy.HasId<TTarget>() && !EntityBaseProxy.IdIsEmpty(target))
        {
            if (existingTargetTracker != null && !existingTargetTracker.StartTracking(target))
            {
                // Only do mapping if the target hasn't been mapped.
                // This will be useful to break from infinite loop caused by navigation properties.
                return;
            }
        }

        var mapperSet = _lookup.LookUp(sourceType, targetType);
        using var ctx = new RecursiveContextPopper(context, sourceType, targetType);
        (mapperSet.customPropertiesMapper as Action<TSource, TTarget>)?.Invoke(source, target);

        if (mapKeyProperties != MapKeyProperties.None)
        {
            (mapperSet.keyMapper as Utilities.MapKeyProperties<TSource, TTarget>)?.Invoke(source, target, _scalarConverter, mapKeyProperties == MapKeyProperties.IdOnly);
        }

        (mapperSet.contentMapper as Utilities.MapProperties<TSource, TTarget, int>)?.Invoke(source, target, _scalarConverter, this, existingTargetTracker, newTargetTracker);
    }
}

internal sealed class ToDatabaseRecursiveMapper : RecursiveMapper
{
    private readonly IScalarTypeConverter _scalarConverter;
    private readonly IEntityFactory _entityFactory;
    private readonly MapperSetLookUp _lookup;
    private readonly EntityBaseProxy _entityBaseProxy;
    private readonly DependentPropertyManager _dependentPropertyManager;
    private readonly KeepUnmatchedManager _keepUnmatchedManager;
    private readonly MapToDatabaseTypeManager _mapToDatabaseTypeManager;
    private readonly DbContext _databaseContext;
    private readonly MappingToDatabaseContext _mappingContext = new ();

    public ToDatabaseRecursiveMapper(
        IScalarTypeConverter scalarConverter,
        IListTypeConstructor listTypeConstructor,
        MapperSetLookUp lookup,
        EntityBaseProxy entityBaseProxy,
        DependentPropertyManager dependentPropertyManager,
        KeepUnmatchedManager keepUnmatchedManager,
        MapToDatabaseTypeManager mapToDatabaseTypeManager,
        IEntityFactory entityFactory,
        DbContext databaseContext)
        : base(scalarConverter, listTypeConstructor, lookup, entityBaseProxy)
    {
        _scalarConverter = scalarConverter;
        _entityFactory = entityFactory;
        _lookup = lookup;
        _dependentPropertyManager = dependentPropertyManager;
        _keepUnmatchedManager = keepUnmatchedManager;
        _mapToDatabaseTypeManager = mapToDatabaseTypeManager;
        _entityBaseProxy = entityBaseProxy;
        _databaseContext = databaseContext;
    }

    public async Task<TTarget> MapAsync<TSource, TTarget>(
        TSource source,
        Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer,
        IExistingTargetTracker? existingTargetTracker,
        INewTargetTracker<int>? newTargetTracker)
        where TSource : class
        where TTarget : class
    {
        const string AsNoTrackingMethodCall = ".AsNoTracking";

        var sourceHasId = _entityBaseProxy.HasId<TSource>() && !_entityBaseProxy.IdIsEmpty(source);
        var mapType = _mapToDatabaseTypeManager.Get<TSource, TTarget>();
        if (sourceHasId)
        {
            TTarget? target;
            var identityEqualsExpression = _entityBaseProxy.GetIdEqualsExpression<TSource, TTarget>(source);
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
                    if (_entityBaseProxy.HasConcurrencyToken<TSource>() && _entityBaseProxy.HasConcurrencyToken<TTarget>()
                        && !_entityBaseProxy.ConcurrencyTokenIsEmpty(source) && !_entityBaseProxy.ConcurrencyTokenIsEmpty(target)
                        && !_entityBaseProxy.ConcurrencyTokenEquals(source, target))
                    {
                        throw new ConcurrencyTokenException(typeof(TSource), typeof(TTarget));
                    }
                    else
                    {
                        Map(source, target, MapKeyProperties.None, existingTargetTracker, newTargetTracker, _mappingContext);
                        return target;
                    }
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

        return MapToNewTarget<TSource, TTarget>(source, sourceHasId ? MapKeyProperties.IdOnly : MapKeyProperties.None, existingTargetTracker, newTargetTracker);
    }

    public override TTarget? MapEntityProperty<TSource, TTarget>(TSource? source, TTarget? target, IExistingTargetTracker existingTargetTracker, INewTargetTracker<int>? newTargetTracker, string propertyName)
        where TSource : class
        where TTarget : class
    {
        if (source == default)
        {
            if (target != default && _dependentPropertyManager.IsDependent(_mappingContext.CurrentTarget, propertyName))
            {
                _databaseContext.Set<TTarget>().Remove(target);
            }

            return default;
        }

        var mapType = _mapToDatabaseTypeManager.Get<TSource, TTarget>();
        if (!EntityBaseProxy.HasId<TSource>() || EntityBaseProxy.IdIsEmpty(source))
        {
            if (!mapType.AllowsInsert())
            {
                throw new MapToDatabaseTypeException(typeof(TSource), typeof(TTarget), MapToDatabaseType.Insert);
            }

            if (target != default && EntityBaseProxy.HasId<TTarget>() && !EntityBaseProxy.IdIsEmpty(target)
                && _dependentPropertyManager.IsDependent(_mappingContext.CurrentTarget, propertyName))
            {
                _databaseContext.Set<TTarget>().Remove(target);
            }

            return MapToNewTarget<TSource, TTarget>(source, MapKeyProperties.None, existingTargetTracker, newTargetTracker);
        }

        if (target != default && !EntityBaseProxy.IdEquals(source, target))
        {
            if (_dependentPropertyManager.IsDependent(_mappingContext.CurrentTarget, propertyName))
            {
                _databaseContext.Set<TTarget>().Remove(target);
            }

            target = FindOrAddTarget<TSource, TTarget>(source, newTargetTracker, mapType);
        }

        if (target == default)
        {
            target = FindOrAddTarget<TSource, TTarget>(source, newTargetTracker, mapType);
        }

        Map(source, target, MapKeyProperties.None, existingTargetTracker, newTargetTracker, _mappingContext);
        return target;
    }

    public override void MapListProperty<TSource, TTarget>(ICollection<TSource>? source, ICollection<TTarget> target, IExistingTargetTracker existingTargetTracker, INewTargetTracker<int>? newTargetTracker, string propertyName)
        where TSource : class
        where TTarget : class
    {
        var shadowSet = new HashSet<TTarget>(target);
        if (source != default)
        {
            var hashCodeSet = new HashSet<int>(source.Count);
            var mapType = _mapToDatabaseTypeManager.Get<TSource, TTarget>();
            var sourceHasIdProperty = EntityBaseProxy.HasId<TSource>();
            var unmatchedSources = new List<TSource>();
            foreach (var s in source)
            {
                if (!hashCodeSet.Add(s.GetHashCode()))
                {
                    throw new DuplicatedListItemException(typeof(TSource));
                }

                if (!sourceHasIdProperty || EntityBaseProxy.IdIsEmpty(s))
                {
                    if (!mapType.AllowsInsert())
                    {
                        throw new MapToDatabaseTypeException(typeof(TSource), typeof(TTarget), MapToDatabaseType.Insert);
                    }

                    target.Add(MapToNewTarget<TSource, TTarget>(s, MapKeyProperties.None, existingTargetTracker, newTargetTracker));
                }
                else
                {
                    var t = target.FirstOrDefault(i => EntityBaseProxy.IdEquals(s, i));
                    if (t != default)
                    {
                        if (!mapType.AllowsUpdate())
                        {
                            throw new MapToDatabaseTypeException(typeof(TSource), typeof(TTarget), MapToDatabaseType.Update);
                        }

                        Map(s, t, MapKeyProperties.None, existingTargetTracker, newTargetTracker, _mappingContext);
                        shadowSet.Remove(t);
                    }
                    else
                    {
                        unmatchedSources.Add(s);
                    }
                }
            }

            if (unmatchedSources.Any())
            {
                var targetsFound = _databaseContext.Set<TTarget>().Where(_entityBaseProxy.GetContainsTargetIdExpression<TSource, TTarget>(unmatchedSources)).ToList();
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
                    var t = targetsFound.Where(t => _entityBaseProxy.IdEquals(s, t)).FirstOrDefault();
                    if (t != default)
                    {
                        Map(s, t, MapKeyProperties.None, existingTargetTracker, newTargetTracker, _mappingContext);
                    }
                    else
                    {
                        t = MapToNewTarget<TSource, TTarget>(s, MapKeyProperties.IdOnly, existingTargetTracker, newTargetTracker);
                    }

                    target.Add(t);
                }
            }
        }

        var current = _mappingContext.Current;
        if (!_keepUnmatchedManager.KeepUnmatched(current.Item1, current.Item2, propertyName))
        {
            var toRemoveFromDatabase = _dependentPropertyManager.IsDependent(current.Item2, propertyName);
            foreach (var toBeRemoved in shadowSet)
            {
                target.Remove(toBeRemoved);
                if (toRemoveFromDatabase)
                {
                    _databaseContext.Set<TTarget>().Remove(toBeRemoved);
                }
            }
        }
    }

    private TTarget MapToNewTarget<TSource, TTarget>(TSource source, MapKeyProperties mapKeyProperties, IExistingTargetTracker? existingTargetTracker, INewTargetTracker<int>? newTargetTracker)
        where TSource : class
        where TTarget : class
    {
        TTarget newTarget;
        bool targetHasBeenMapped;
        if (newTargetTracker != null)
        {
            targetHasBeenMapped = newTargetTracker.NewTargetIfNotExist(source.GetHashCode(), out newTarget);
        }
        else
        {
            newTarget = _entityFactory.Make<TTarget>();
            targetHasBeenMapped = false;
        }

        if (!targetHasBeenMapped)
        {
            Map(source, newTarget, mapKeyProperties, existingTargetTracker, newTargetTracker, _mappingContext);
            _databaseContext.Set<TTarget>().Add(newTarget);
        }

        return newTarget;
    }

    private TTarget FindOrAddTarget<TSource, TTarget>(TSource source, INewTargetTracker<int>? newTargetTracker, MapToDatabaseType mapType)
        where TSource : class
        where TTarget : class
    {
        var identityEqualsExpression = _entityBaseProxy.GetIdEqualsExpression<TSource, TTarget>(source);
        var target = _databaseContext.Set<TTarget>().FirstOrDefault(identityEqualsExpression);
        if (target == null)
        {
            if (!mapType.AllowsInsert())
            {
                throw new MapToDatabaseTypeException(typeof(TSource), typeof(TTarget), MapToDatabaseType.Insert);
            }

            if (newTargetTracker != null)
            {
                if (!newTargetTracker.NewTargetIfNotExist(source.GetHashCode(), out TTarget newTarget))
                {
                    _databaseContext.Set<TTarget>().Add(newTarget);
                }

                target = newTarget;
            }
            else
            {
                target = _entityFactory.Make<TTarget>();
                _databaseContext.Set<TTarget>().Add(target);
            }
        }
        else if (!mapType.AllowsUpdate())
        {
            throw new MapToDatabaseTypeException(typeof(TSource), typeof(TTarget), MapToDatabaseType.Update);
        }

        var mapperSet = _lookup.LookUp(typeof(TSource), typeof(TTarget));
        if (mapperSet.keyMapper != null)
        {
            // when entered this method, source is guaranteed to have an id
            ((Utilities.MapKeyProperties<TSource, TTarget>)mapperSet.keyMapper)(source, target, _scalarConverter, true);
        }

        return target;
    }
}

internal sealed class ToMemoryRecursiveMapper : RecursiveMapper
{
    private readonly IEntityFactory _entityFactory;

    public ToMemoryRecursiveMapper(
        IScalarTypeConverter scalarConverter,
        IListTypeConstructor listTypeConstructor,
        MapperSetLookUp lookup,
        EntityBaseProxy entityBaseProxy,
        IEntityFactory entityFactory)
        : base(scalarConverter, listTypeConstructor, lookup, entityBaseProxy)
    {
        _entityFactory = entityFactory;
    }

    public override TTarget? MapEntityProperty<TSource, TTarget>(TSource? source, TTarget? target, IExistingTargetTracker existingTargetTracker, INewTargetTracker<int>? newTargetTracker, string propertyName)
        where TSource : class
        where TTarget : class
    {
        if (source != default)
        {
            if (target != default)
            {
                Map(source, target, MapKeyProperties.IdAndConcurrencyToken, existingTargetTracker, newTargetTracker);
                return target;
            }

            return MapToNewTarget<TSource, TTarget>(source, existingTargetTracker, newTargetTracker);
        }

        return default;
    }

    public override void MapListProperty<TSource, TTarget>(ICollection<TSource>? source, ICollection<TTarget> target, IExistingTargetTracker existingTargetTracker, INewTargetTracker<int>? newTargetTracker, string propertyName)
    {
        if (source != default)
        {
            var hashCodeSet = new HashSet<int>(source.Count);
            foreach (var s in source)
            {
                if (!hashCodeSet.Add(s.GetHashCode()))
                {
                    throw new DuplicatedListItemException(typeof(TSource));
                }

                target.Add(MapToNewTarget<TSource, TTarget>(s, existingTargetTracker, newTargetTracker));
            }
        }
    }

    private TTarget MapToNewTarget<TSource, TTarget>(TSource source, IExistingTargetTracker existingTargetTracker, INewTargetTracker<int>? newTargetTracker)
        where TSource : class
        where TTarget : class
    {
        TTarget newTarget;
        bool targetHasBeenMapped;
        if (newTargetTracker != null)
        {
            targetHasBeenMapped = newTargetTracker.NewTargetIfNotExist(source.GetHashCode(), out newTarget);
        }
        else
        {
            newTarget = _entityFactory.Make<TTarget>();
            targetHasBeenMapped = false;
        }

        if (!targetHasBeenMapped)
        {
            Map(source, newTarget, MapKeyProperties.IdAndConcurrencyToken, existingTargetTracker, newTargetTracker);
        }

        return newTarget;
    }
}