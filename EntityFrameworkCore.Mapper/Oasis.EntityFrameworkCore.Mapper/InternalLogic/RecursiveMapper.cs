﻿namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

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
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> mappers,
        EntityBaseProxy entityBaseProxy)
    {
        NewTargetTracker = newTargetTracker;
        _scalarConverters = scalarConverters;
        _mappers = mappers;
        EntityBaseProxy = entityBaseProxy;
    }

    protected NewTargetTracker<T> NewTargetTracker { get; init; }

    protected EntityBaseProxy EntityBaseProxy { get; init; }

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
        where TSource : class
        where TTarget : class, new();

    public abstract void MapListProperty<TSource, TTarget>(ICollection<TSource>? source, ICollection<TTarget> target)
        where TSource : class
        where TTarget : class, new();

    internal void Map<TSource, TTarget>(TSource source, TTarget target)
        where TSource : class
        where TTarget : class
    {
        var targetType = typeof(TTarget);
        var targetTypeIsTracked = _trackerDictionary.TryGetValue(targetType, out var existingTargetTracker);

        if (!EntityBaseProxy.IdIsEmpty(target))
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

internal sealed class ToDatabaseRecursiveMapper : RecursiveMapper<int>
{
    private readonly DbContext _databaseContext;

    public ToDatabaseRecursiveMapper(
        NewTargetTracker<int> newTargetTracker,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> scalarConverters,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> mappers,
        EntityBaseProxy entityBaseProxy,
        DbContext databaseContext)
        : base(newTargetTracker, scalarConverters, mappers, entityBaseProxy)
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
                EntityBaseProxy.HandleRemove(_databaseContext, target);
            }

            return default;
        }

        if (EntityBaseProxy.IdIsEmpty(source))
        {
            if (target != default && !EntityBaseProxy.IdIsEmpty(target))
            {
                EntityBaseProxy.HandleRemove(_databaseContext, target);
            }

            if (!NewTargetTracker.NewTargetIfNotExist<TTarget>(source.GetHashCode(), out var n))
            {
                Map(source, n!);
                _databaseContext.Set<TTarget>().Add(n!);
            }

            return n;
        }

        var identityEqualsExpression = Utilities.BuildIdEqualsExpression<TTarget>(EntityBaseProxy, EntityBaseProxy.GetId(source));
        if (target == default)
        {
            target = _databaseContext.Set<TTarget>().FirstOrDefault(identityEqualsExpression);
        }
        else if (!EntityBaseProxy.IdEquals(source, target))
        {
            EntityBaseProxy.HandleRemove(_databaseContext, target);
            target = _databaseContext.Set<TTarget>().FirstOrDefault(identityEqualsExpression);
        }

        if (target == default)
        {
            throw new EntityNotFoundException(typeof(TTarget), EntityBaseProxy.GetId(source));
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
                if (EntityBaseProxy.IdIsEmpty(s))
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
                    var t = target.SingleOrDefault(i => Equals(EntityBaseProxy.GetId(i), EntityBaseProxy.GetId(s)));
                    if (t != default)
                    {
                        if (EntityBaseProxy.TimeStampIsEmpty(s) || !EntityBaseProxy.TimeStampEquals(s, t))
                        {
                            throw new StaleEntityException(typeof(TTarget), EntityBaseProxy.GetId(s));
                        }

                        Map(s, t);
                        shadowSet.Remove(t);
                    }
                    else
                    {
                        throw new EntityNotFoundException(typeof(TTarget), EntityBaseProxy.GetId(s));
                    }
                }
            }
        }

        foreach (var toBeRemoved in shadowSet)
        {
            target.Remove(toBeRemoved);
            EntityBaseProxy.HandleRemove(_databaseContext, toBeRemoved);
        }
    }
}

internal sealed class ToMemoryRecursiveMapper : RecursiveMapper<int>
{
    public ToMemoryRecursiveMapper(
        NewTargetTracker<int> newTargetTracker,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> scalarConverters,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> mappers,
        EntityBaseProxy entityBaseProxy)
        : base(newTargetTracker, scalarConverters, mappers, entityBaseProxy)
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