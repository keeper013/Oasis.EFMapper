using Oasis.EfMapper.Exceptions;

namespace Oasis.EfMapper;

internal static class Utilities
{
    public delegate void MapScalarProperties<TSource, TTarget>(TSource source, TTarget target)
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase;

    public delegate void MapListProperties<TSource, TTarget>(TSource source, TTarget target, MappingContext context)
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase;

    public static void RecursivelyMap<TSource, TTarget>(TSource source, TTarget target, MappingContext context)
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase
    {
        var targetType = typeof(TTarget);
        var targetTypeHasMaps = context.TrackerDictionary.TryGetValue(targetType, out var tracker);
        var targetAlreadyExists = target.Id.HasValue;
        var targetIsMapped = targetTypeHasMaps && ((targetAlreadyExists && tracker!.IdSet.Contains(target.Id!.Value)) || (!targetAlreadyExists && tracker!.HashCodeSet.Contains(target.GetHashCode())));

        // only do scalar property mapping if the target hasn't been mapped
        if (targetIsMapped)
        {
            return;
        }

        var mapperSet = context.GetMapperSet<TSource, TTarget>();
        if (!mapperSet.HasValue)
        {
            throw new ArgumentException($"Entity mapper from type {typeof(TSource)} to {targetType} hasn't been registered yet.");
        }

        ((MapScalarProperties<TSource, TTarget>)mapperSet.Value.ScalarPropertiesMapper)(source, target);

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
            context.TrackerDictionary.Add(targetType, tracker);
        }

        // after target type is marked as mapped, go on to map collections
        ((MapListProperties<TSource, TTarget>)mapperSet.Value.ListPropertiesMapper)(source, target, context);
    }

    public static void MapListProperty<TSource, TTarget>(
        ICollection<TSource> source, ICollection<TTarget> target, MappingContext context)
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase, new()
    {
        var ids = new HashSet<long>(target.Select(i => i.Id!.Value));
        if (source != null)
        {
            foreach (var s in source)
            {
                if (s.Id == null)
                {
                    var n = new TTarget();
                    RecursivelyMap(s, n, context);
                    target.Add(n);
                    context.DbContext.Add(n);
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

                        RecursivelyMap(s, t, context);
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
            context.DbContext.Remove(t);
        }
    }
}
