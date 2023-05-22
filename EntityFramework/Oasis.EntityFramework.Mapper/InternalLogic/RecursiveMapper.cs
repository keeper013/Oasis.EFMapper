namespace Oasis.EntityFramework.Mapper.InternalLogic;

using Oasis.EntityFramework.Mapper.Exceptions;
using System.Data.Entity;

internal abstract class RecursiveMapper<T> : IEntityPropertyMapper, IListPropertyMapper
    where T : struct
{
    private readonly MapperSetLookUp _lookup;
    private readonly IScalarTypeConverter _scalarConverter;
    private readonly IListTypeConstructor _listTypeConstructor;
    private readonly IDictionary<Type, ExistingTargetTracker> _trackerDictionary = new Dictionary<Type, ExistingTargetTracker>();

    internal RecursiveMapper(
        NewTargetTracker<T> newTargetTracker,
        IScalarTypeConverter scalarConverter,
        IListTypeConstructor listTypeConstructor,
        MapperSetLookUp lookup,
        EntityBaseProxy entityBaseProxy)
    {
        NewTargetTracker = newTargetTracker;
        _scalarConverter = scalarConverter;
        _listTypeConstructor = listTypeConstructor;
        _lookup = lookup;
        EntityBaseProxy = entityBaseProxy;
    }

    protected NewTargetTracker<T> NewTargetTracker { get; }

    protected EntityBaseProxy EntityBaseProxy { get; }

    public abstract TTarget? MapEntityProperty<TSource, TTarget>(TSource? source, TTarget? target)
        where TSource : class
        where TTarget : class;

    public abstract void MapListProperty<TSource, TTarget>(ICollection<TSource>? source, ICollection<TTarget> target)
        where TSource : class
        where TTarget : class;

    TList IListPropertyMapper.ConstructListType<TList, TItem>()
    {
        return _listTypeConstructor.Construct<TList, TItem>();
    }

    internal void Map<TSource, TTarget>(TSource source, TTarget target, bool mapKeyProperties)
        where TSource : class
        where TTarget : class
    {
        var targetType = typeof(TTarget);
        var targetTypeIsTracked = _trackerDictionary.TryGetValue(targetType, out var existingTargetTracker);

        if (EntityBaseProxy.HasId<TTarget>() && !EntityBaseProxy.IdIsEmpty(target))
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

        var mapperSet = _lookup.LookUp(typeof(TSource), typeof(TTarget));
        if (mapperSet.customPropertiesMapper != null)
        {
            ((Utilities.MapCustomProperties<TSource, TTarget>)mapperSet.customPropertiesMapper)(source, target);
        }

        if (mapKeyProperties)
        {
            ((Utilities.MapScalarProperties<TSource, TTarget>)mapperSet.keyPropertiesMapper)(source, target, _scalarConverter);
        }

        ((Utilities.MapScalarProperties<TSource, TTarget>)mapperSet.scalarPropertiesMapper)(source, target, _scalarConverter);
        ((Utilities.MapEntityProperties<TSource, TTarget>)mapperSet.entityPropertiesMapper)(source, target, this);
        ((Utilities.MapListProperties<TSource, TTarget>)mapperSet.listPropertiesMapper)(source, target, this);
    }

    private class ExistingTargetTracker
    {
        private readonly ISet<int> _existingTargetHashCodeSet = new HashSet<int>();

        public bool StartTracking(int hashCode) => _existingTargetHashCodeSet.Add(hashCode);
    }
}

internal sealed class ToDatabaseRecursiveMapper : RecursiveMapper<int>
{
    private readonly IScalarTypeConverter _scalarConverter;
    private readonly IEntityFactory _entityFactory;
    private readonly MapperSetLookUp _lookup;
    private readonly DbContext _databaseContext;

    public ToDatabaseRecursiveMapper(
        NewTargetTracker<int> newTargetTracker,
        IScalarTypeConverter scalarConverter,
        IListTypeConstructor listTypeConstructor,
        IEntityFactory entityFactory,
        MapperSetLookUp lookup,
        EntityBaseProxy entityBaseProxy,
        DbContext databaseContext)
        : base(newTargetTracker, scalarConverter, listTypeConstructor, lookup, entityBaseProxy)
    {
        _scalarConverter = scalarConverter;
        _entityFactory = entityFactory;
        _lookup = lookup;
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
                Map(source, n, false);
                _databaseContext.Set<TTarget>().Add(n);
            }

            return n;
        }

        if (target != default && !EntityBaseProxy.IdEquals(source, target))
        {
            EntityBaseProxy.HandleRemove(_databaseContext, target);
            target = AttachTarget<TSource, TTarget>(source);
        }

        if (target == default)
        {
            target = AttachTarget<TSource, TTarget>(source);
        }

        Map(source, target, false);
        return target;
    }

    public override void MapListProperty<TSource, TTarget>(ICollection<TSource>? source, ICollection<TTarget> target)
        where TSource : class
        where TTarget : class
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
                        Map(s, n, false);
                        _databaseContext.Set<TTarget>().Add(n);
                    }

                    target.Add(n);
                }
                else
                {
                    var t = target.FirstOrDefault(i => Equals(EntityBaseProxy.GetId(i), EntityBaseProxy.GetId(s)));
                    if (t != default)
                    {
                        Map(s, t, false);
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

    private TTarget AttachTarget<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class
    {
        var target = _entityFactory.Make<TTarget>();
        var mapperSet = _lookup.LookUp(typeof(TSource), typeof(TTarget));
        ((Utilities.MapScalarProperties<TSource, TTarget>)mapperSet.keyPropertiesMapper)(source, target, _scalarConverter);
        try
        {
            _databaseContext.Set<TTarget>().Attach(target);
        }
        catch (InvalidOperationException e)
        {
            throw new AttachToDbSetException(e);
        }

        return target;
    }
}

internal sealed class ToMemoryRecursiveMapper : RecursiveMapper<int>
{
    public ToMemoryRecursiveMapper(
        NewTargetTracker<int> newTargetTracker,
        IScalarTypeConverter scalarConverter,
        IListTypeConstructor listTypeConstructor,
        MapperSetLookUp lookup,
        EntityBaseProxy entityBaseProxy)
        : base(newTargetTracker, scalarConverter, listTypeConstructor, lookup, entityBaseProxy)
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
                Map(source, n, true);
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
                    Map(s, n, true);
                }

                target.Add(n);
            }
        }
    }
}