namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Exceptions;

internal abstract class RecursiveMapper<T> : IScalarTypeConverter, IEntityPropertyMapper, IListPropertyMapper
    where T : struct
{
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> _mappers;
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> _scalarConverters;
    private readonly IDictionary<Type, ExistingTargetTracker> _trackerDictionary = new Dictionary<Type, ExistingTargetTracker>();

    internal RecursiveMapper(
        NewTargetTracker<T> newTargetTracker,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> scalarConverters,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> mappers)
    {
        NewTargetTracker = newTargetTracker;
        _scalarConverters = scalarConverters;
        _mappers = mappers;
    }

    protected NewTargetTracker<T> NewTargetTracker { get; init; }

    public TTarget? Convert<TSource, TTarget>(TSource? source)
        where TSource : notnull
        where TTarget : notnull
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);
        if (!_scalarConverters.TryGetValue(sourceType, out var innerDictionary) || !innerDictionary.TryGetValue(targetType, out var converter))
        {
            throw new ScalarConverterMissingException(sourceType, targetType);
        }

        return ((Func<TSource?, TTarget?>)converter)(source);
    }

    public abstract TTarget? MapEntityProperty<TSource, TTarget>(TSource? source, TTarget? target)
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase, new();

    public abstract void MapListProperty<TSource, TTarget>(ICollection<TSource>? source, ICollection<TTarget> target)
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase, new();

    internal void Map<TSource, TTarget>(TSource source, TTarget target)
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase
    {
        var targetType = typeof(TTarget);
        var targetTypeIsTracked = _trackerDictionary.TryGetValue(targetType, out var existingTargetTracker);

        if (target.Id.HasValue)
        {
            if (!targetTypeIsTracked)
            {
                existingTargetTracker = new ExistingTargetTracker();
                _trackerDictionary.Add(targetType, existingTargetTracker);
            }

            if (!existingTargetTracker!.StartTracking(target.GetHashCode()))
            {
                // Only do mapping if the target hasn't been mapped.
                // This will be useful to break from infinite loop caused by navigation properties.
                return;
            }
        }

        MapperSet mapperSet = default;
        var mapperSetFound = _mappers.TryGetValue(typeof(TSource), out var innerDictionary)
            && innerDictionary.TryGetValue(typeof(TTarget), out mapperSet);
        if (!mapperSetFound)
        {
            throw new ArgumentException($"Entity mapper from type {typeof(TSource)} to {targetType} hasn't been registered yet.");
        }

        ((Utilities.MapScalarProperties<TSource, TTarget>)mapperSet.scalarPropertiesMapper)(source, target, this);
        ((Utilities.MapEntityProperties<TSource, TTarget>)mapperSet.entityPropertiesMapper)(source, target, this);
        ((Utilities.MapListProperties<TSource, TTarget>)mapperSet.listPropertiesMapper)(source, target, this);
    }

    private class ExistingTargetTracker
    {
        private readonly ISet<int> _existingTargetIdSet = new HashSet<int>();

        public bool StartTracking(int hashCode) => _existingTargetIdSet.Add(hashCode);
    }
}

internal sealed class ToEntitiesRecursiveMapper : RecursiveMapper<int>
{
    private readonly DbContext _databaseContext;

    public ToEntitiesRecursiveMapper(
        NewTargetTracker<int> newTargetTracker,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> scalarConverters,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> mappers,
        DbContext databaseContext)
        : base(newTargetTracker, scalarConverters, mappers)
    {
        _databaseContext = databaseContext;
    }

    public override TTarget? MapEntityProperty<TSource, TTarget>(TSource? source, TTarget? target)
        where TSource : class
        where TTarget : class
    {
        if (source == default)
        {
            if (target != default)
            {
                _databaseContext.Set<TTarget>().Remove(target);
            }

            return default;
        }

        if (!source.Id.HasValue)
        {
            if (target != default && target.Id.HasValue)
            {
                _databaseContext.Set<TTarget>().Remove(target);
            }

            if (!NewTargetTracker.NewTargetIfNotExist<TTarget>(source.GetHashCode(), out var n))
            {
                Map(source, n!);
                _databaseContext.Set<TTarget>().Add(n!);
            }

            return n;
        }

        if (target == default || source.Id != target.Id)
        {
            throw new EntityNotFoundException(typeof(TTarget), source.Id);
        }

        Map(source, target);
        return target;
    }

    public override void MapListProperty<TSource, TTarget>(ICollection<TSource>? source, ICollection<TTarget> target)
    {
        var shadowSet = new HashSet<TTarget>(target);
        if (source != default)
        {
            foreach (var s in source)
            {
                if (!s.Id.HasValue)
                {
                    if (!NewTargetTracker.NewTargetIfNotExist<TTarget>(s.GetHashCode(), out var n))
                    {
                        Map(s, n!);
                        _databaseContext.Set<TTarget>().Add(n!);
                    }

                    target.Add(n!);
                }
                else
                {
                    var t = target.SingleOrDefault(i => i.Id == s.Id);
                    if (t != default)
                    {
                        if (s.Timestamp == default || !Enumerable.SequenceEqual(s.Timestamp, t.Timestamp!))
                        {
                            throw new StaleEntityException(typeof(TTarget), s.Id);
                        }

                        Map(s, t);
                        shadowSet.Remove(t);
                    }
                    else
                    {
                        throw new EntityNotFoundException(typeof(TTarget), s.Id);
                    }
                }
            }
        }

        foreach (var toBeRemoved in shadowSet)
        {
            target.Remove(toBeRemoved);

            // TODO: add class specific configuration for this, removing should be default option
            // this mapper believes that all navigation properties in collections are relationship entities,
            // that if the entity is removed from the collection, it should be directly removed from the database.
            // Without this assumption, users will need to specify logic of deletion for every one-to-many navigation properties,
            // which is unpractical and troublesome
            _databaseContext.Set<TTarget>().Remove(toBeRemoved);
        }
    }
}

internal sealed class FromEntitiesRecursiveMapper : RecursiveMapper<int>
{
    public FromEntitiesRecursiveMapper(
        NewTargetTracker<int> newTargetTracker,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> scalarConverters,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> mappers)
        : base(newTargetTracker, scalarConverters, mappers)
    {
    }

    public override TTarget? MapEntityProperty<TSource, TTarget>(TSource? source, TTarget? target)
        where TSource : class
        where TTarget : class
    {
        if (source != default)
        {
            if (!NewTargetTracker.NewTargetIfNotExist<TTarget>(source.GetHashCode(), out var n))
            {
                Map(source, n!);
            }

            return n;
        }

        return default;
    }

    public override void MapListProperty<TSource, TTarget>(ICollection<TSource>? source, ICollection<TTarget> target)
    {
        if (source != default)
        {
            foreach (var s in source)
            {
                if (!NewTargetTracker.NewTargetIfNotExist<TTarget>(s.GetHashCode(), out var n))
                {
                    Map(s, n!);
                }

                target.Add(n!);
            }
        }
    }
}