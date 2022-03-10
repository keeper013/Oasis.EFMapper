namespace Oasis.EntityFrameworkCore.Mapper;

using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Exceptions;

internal class RecursiveMapper : IListPropertyMapper
{
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> _mappers;
    private readonly IDictionary<Type, TargetTracker> _trackerDictionary = new Dictionary<Type, TargetTracker>();
    private readonly DbContext _dbContext;

    internal RecursiveMapper(
        DbContext dbContext,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> mappers)
    {
        _dbContext = dbContext;
        _mappers = mappers;
    }

    internal void Map<TSource, TTarget>(TSource source, TTarget target)
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase
    {
        var targetType = typeof(TTarget);
        var targetTypeHasMaps = _trackerDictionary.TryGetValue(targetType, out var tracker);
        var targetAlreadyExists = target.Id.HasValue;
        var targetIsMapped = targetTypeHasMaps && ((targetAlreadyExists && tracker!.IdSet.Contains(target.Id!.Value)) || (!targetAlreadyExists && tracker!.HashCodeSet.Contains(target.GetHashCode())));

        // only do scalar property mapping if the target hasn't been mapped
        if (targetIsMapped)
        {
            return;
        }

        MapperSet mapperSet = new MapperSet();
        var mapperSetFound = _mappers.TryGetValue(typeof(TSource), out var innerDictionary)
            && innerDictionary.TryGetValue(typeof(TTarget), out mapperSet);
        if (!mapperSetFound)
        {
            throw new ArgumentException($"Entity mapper from type {typeof(TSource)} to {targetType} hasn't been registered yet.");
        }

        ((Utilities.MapScalarProperties<TSource, TTarget>)mapperSet.ScalarPropertiesMapper)(source, target);

        // after scalar property mapping, add target as mapped, to break out from recursive situation
        if (!targetTypeHasMaps)
        {
            tracker = new TargetTracker();
        }

        if (target.Id.HasValue)
        {
            tracker!.IdSet.Add(target.Id.Value);
        }
        else
        {
            tracker!.HashCodeSet.Add(target.GetHashCode());
        }

        if (!targetTypeHasMaps)
        {
            _trackerDictionary.Add(targetType, tracker);
        }

        // after target type is marked as mapped, go on to map collections
        ((Utilities.MapListProperties<TSource, TTarget>)mapperSet.ListPropertiesMapper)(source, target, this);
    }

    void IListPropertyMapper.MapListProperty<TSource, TTarget>(ICollection<TSource> source, ICollection<TTarget> target)
    {
        var ids = new HashSet<long>(target.Select(i => i.Id!.Value));
        if (source != null)
        {
            foreach (var s in source)
            {
                if (s.Id == null)
                {
                    var n = new TTarget();
                    Map(s, n);
                    target.Add(n);
                    _dbContext.Set<TTarget>().Add(n);
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
            _dbContext.Set<TTarget>().Remove(t);
        }
    }

    private class TargetTracker
    {
        public ISet<long> IdSet { get; } = new HashSet<long>();

        public ISet<int> HashCodeSet { get; } = new HashSet<int>();
    }
}

internal record struct MapperSet(Delegate ScalarPropertiesMapper, Delegate ListPropertiesMapper);
