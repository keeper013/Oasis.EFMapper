namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Exceptions;

internal abstract class RecursiveMapper : IListPropertyMapper
{
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> _mappers;
    private readonly IDictionary<Type, ExistingTargetTracker> _trackerDictionary = new Dictionary<Type, ExistingTargetTracker>();

    internal RecursiveMapper(
        NewEntityTracker newSourceEntityTracker,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> mappers)
    {
        NewSourceEntityTracker = newSourceEntityTracker;
        _mappers = mappers;
    }

    protected NewEntityTracker NewSourceEntityTracker { get; init; }

    public abstract void MapListProperty<TSource, TTarget>(ICollection<TSource> source, ICollection<TTarget> target)
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

            if (!existingTargetTracker!.StartTracking(target.Id.Value))
            {
                // only do property mapping if the target hasn't been mapped
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

        ((Utilities.MapScalarProperties<TSource, TTarget>)mapperSet.scalarPropertiesMapper)(source, target);
        ((Utilities.MapListProperties<TSource, TTarget>)mapperSet.listPropertiesMapper)(source, target, this);
    }

    private class ExistingTargetTracker
    {
        private ISet<long> _existingTargetIdSet = new HashSet<long>();

        public bool StartTracking(long id) => _existingTargetIdSet.Add(id);
    }
}

internal sealed class ToEntitiesRecursiveMapper : RecursiveMapper
{
    private readonly DbContext _databaseContext;

    public ToEntitiesRecursiveMapper(
        NewEntityTracker newSourceEntityTracker,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> mappers,
        DbContext databaseContext)
        : base(newSourceEntityTracker, mappers)
    {
        _databaseContext = databaseContext;
    }

    public override void MapListProperty<TSource, TTarget>(ICollection<TSource> source, ICollection<TTarget> target)
    {
        var ids = new HashSet<long>(target.Select(i => i.Id!.Value));
        if (source != null)
        {
            foreach (var s in source)
            {
                if (s.Id == null)
                {
                    if (!NewSourceEntityTracker.NewTargetIfNotExist<TTarget>(s.GetHashCode(), out var n))
                    {
                        Map(s, n!);
                        _databaseContext.Set<TTarget>().Add(n!);
                    }

                    target.Add(n!);
                }
                else
                {
                    var t = target.SingleOrDefault(i => i.Id == s.Id);
                    if (t != null)
                    {
                        if (s.Timestamp == null || !Enumerable.SequenceEqual(s.Timestamp, t.Timestamp!))
                        {
                            throw new StaleEntityException(typeof(TTarget), s.Id.Value);
                        }

                        Map(s, t);
                        ids.Remove(s.Id.Value);
                    }
                    else
                    {
                        throw new EntityNotFoundException(typeof(TTarget), s.Id.Value);
                    }
                }
            }
        }

        foreach (var id in ids)
        {
            var t = target.Single(t => t.Id == id);
            target.Remove(t);
            var t1 = _databaseContext.Set<TTarget>().Single(t => t.Id == id);
            _databaseContext.Set<TTarget>().Remove(t1);
        }
    }
}

internal sealed class FromEntitiesRecursiveMapper : RecursiveMapper
{
    public FromEntitiesRecursiveMapper(
        NewEntityTracker newSourceEntityTracker,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> mappers)
        : base(newSourceEntityTracker, mappers)
    {
    }

    public override void MapListProperty<TSource, TTarget>(ICollection<TSource> source, ICollection<TTarget> target)
    {
        if (source != null)
        {
            foreach (var s in source)
            {
                if (!NewSourceEntityTracker.NewTargetIfNotExist<TTarget>(s.GetHashCode(), out var n))
                {
                    Map(s, n!);
                }

                target.Add(n!);
            }
        }
    }
}