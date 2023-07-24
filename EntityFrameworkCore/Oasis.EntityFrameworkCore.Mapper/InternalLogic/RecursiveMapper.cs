namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Exceptions;

internal abstract class RecursiveMapper : IRecursiveMapper<int>
{
    private readonly MapperSetLookUp _lookup;
    private readonly IScalarTypeConverter _scalarConverter;
    private readonly IListTypeConstructor _listTypeConstructor;
    private readonly IDictionary<Type, ExistingTargetTracker> _trackerDictionary = new Dictionary<Type, ExistingTargetTracker>();

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

    protected EntityBaseProxy EntityBaseProxy { get; init; }

    public abstract TTarget? MapEntityProperty<TSource, TTarget>(TSource? source, TTarget? target, INewTargetTracker<int>? newTargetTracker, string propertyName, bool? keepUnmatched)
        where TSource : class
        where TTarget : class;

    public abstract void MapListProperty<TSource, TTarget>(ICollection<TSource>? source, ICollection<TTarget> target, INewTargetTracker<int>? newTargetTracker, string propertyName, bool? keepUnmatched)
        where TSource : class
        where TTarget : class;

    public TList ConstructListType<TList, TItem>()
        where TList : class, ICollection<TItem>
        where TItem : class
    {
        return _listTypeConstructor.Construct<TList, TItem>();
    }

    internal void Map<TSource, TTarget>(TSource source, TTarget target, bool mapKeyProperties, INewTargetTracker<int>? newTargetTracker, MappingToDatabaseContext? context = null, bool? keepUnmatched = null)
        where TSource : class
        where TTarget : class
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);

        if (EntityBaseProxy.HasId<TTarget>() && !EntityBaseProxy.IdIsEmpty(target))
        {
            if (!_trackerDictionary.TryGetValue(targetType, out var existingTargetTracker))
            {
                existingTargetTracker = new ExistingTargetTracker();
                _trackerDictionary.Add(targetType, existingTargetTracker);
            }

            if (!existingTargetTracker.StartTracking(target.GetHashCode()))
            {
                // Only do mapping if the target hasn't been mapped.
                // This will be useful to break from infinite loop caused by navigation properties.
                return;
            }
        }

        var mapperSet = _lookup.LookUp(sourceType, targetType);
        if (!mapperSet.HasValue)
        {
            return;
        }

        using var ctx = new RecursiveContextPopper(context, sourceType, targetType);
        var value = mapperSet.Value;
        if (value.customPropertiesMapper != null)
        {
            ((Action<TSource, TTarget>)value.customPropertiesMapper)(source, target);
        }

        if (value.keyMapper != null && mapKeyProperties)
        {
            ((Utilities.MapScalarProperties<TSource, TTarget>)value.keyMapper)(source, target, _scalarConverter);
        }

        if (value.contentMapper != null)
        {
            ((Utilities.MapProperties<TSource, TTarget, int>)value.contentMapper)(source, target, _scalarConverter, this, newTargetTracker, keepUnmatched);
        }
    }

    private class ExistingTargetTracker
    {
        private readonly ISet<int> _existingTargetHashCodeSet = new HashSet<int>();

        public bool StartTracking(int hashCode) => _existingTargetHashCodeSet.Add(hashCode);
    }
}

internal sealed class ToDatabaseRecursiveMapper : RecursiveMapper
{
    private readonly IScalarTypeConverter _scalarConverter;
    private readonly IEntityFactory _entityFactory;
    private readonly MapperSetLookUp _lookup;
    private readonly EntityBaseProxy _entityBaseProxy;
    private readonly EntityRemover _entityRemover;
    private readonly DbContext _databaseContext;
    private readonly MappingToDatabaseContext _mappingContext = new ();

    public ToDatabaseRecursiveMapper(
        IScalarTypeConverter scalarConverter,
        IListTypeConstructor listTypeConstructor,
        MapperSetLookUp lookup,
        EntityBaseProxy entityBaseProxy,
        EntityRemover entityRemover,
        IEntityFactory entityFactory,
        DbContext databaseContext)
        : base(scalarConverter, listTypeConstructor, lookup, entityBaseProxy)
    {
        _scalarConverter = scalarConverter;
        _entityFactory = entityFactory;
        _lookup = lookup;
        _entityRemover = entityRemover;
        _entityBaseProxy = entityBaseProxy;
        _databaseContext = databaseContext;
    }

    public async Task<TTarget> MapAsync<TSource, TTarget>(
        TSource source,
        Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer,
        MapToDatabaseType mappingType,
        INewTargetTracker<int>? newTargetTracker,
        bool? keepUnmatched)
        where TSource : class
        where TTarget : class
    {
        const string AsNoTrackingMethodCall = ".AsNoTracking";

        var sourceHasId = _entityBaseProxy.HasId<TSource>() && !_entityBaseProxy.IdIsEmpty(source);
        if (sourceHasId)
        {
            TTarget? target;
            var id = _entityBaseProxy.GetId(source);
            var identityEqualsExpression = BuildIdEqualsExpression<TTarget>(_entityBaseProxy, _scalarConverter, id);
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
                if (mappingType == MapToDatabaseType.Insert)
                {
                    throw new InsertToDatabaseWithExistingException();
                }
                else
                {
                    if (_entityBaseProxy.HasConcurrencyToken<TSource>() && _entityBaseProxy.HasConcurrencyToken<TTarget>()
                        && !_entityBaseProxy.ConcurrencyTokenIsEmpty(source) && !_entityBaseProxy.ConcurrencyTokenIsEmpty(target)
                        && !_entityBaseProxy.ConcurrencyTokenEquals(source, target))
                    {
                        throw new ConcurrencyTokenException(typeof(TSource), typeof(TTarget), id);
                    }
                    else
                    {
                        Map(source, target, true, newTargetTracker, _mappingContext, keepUnmatched);
                        return target;
                    }
                }
            }
        }

        if (mappingType == MapToDatabaseType.Update)
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

        return MapToNewTarget<TSource, TTarget>(source, sourceHasId, newTargetTracker, keepUnmatched);
    }

    public override TTarget? MapEntityProperty<TSource, TTarget>(TSource? source, TTarget? target, INewTargetTracker<int>? newTargetTracker, string propertyName, bool? keepUnmatched)
        where TSource : class
        where TTarget : class
    {
        if (source == default)
        {
            if (target != default)
            {
                _entityRemover.RemoveIfConfigured(_databaseContext, target, _mappingContext.MakeMappingData(propertyName));
            }

            return default;
        }

        if (EntityBaseProxy.IdIsEmpty(source))
        {
            if (target != default && !EntityBaseProxy.IdIsEmpty(target))
            {
                _entityRemover.RemoveIfConfigured(_databaseContext, target, _mappingContext.MakeMappingData(propertyName));
            }

            return MapToNewTarget<TSource, TTarget>(source, false, newTargetTracker, keepUnmatched);
        }

        if (target != default && !EntityBaseProxy.IdEquals(source, target))
        {
            _entityRemover.RemoveIfConfigured(_databaseContext, target, _mappingContext.MakeMappingData(propertyName));
            target = AttachTarget<TSource, TTarget>(source);
        }

        if (target == default)
        {
            target = AttachTarget<TSource, TTarget>(source);
        }

        Map(source, target, false, newTargetTracker, _mappingContext, keepUnmatched);
        return target;
    }

    public override void MapListProperty<TSource, TTarget>(ICollection<TSource>? source, ICollection<TTarget> target, INewTargetTracker<int>? newTargetTracker, string propertyName, bool? keepUnmatched)
        where TSource : class
        where TTarget : class
    {
        var shadowSet = new HashSet<TTarget>(target);
        if (source != default)
        {
            var hashCodeSet = new HashSet<int>(source.Count);
            foreach (var s in source)
            {
                if (!hashCodeSet.Add(s.GetHashCode()))
                {
                    throw new DuplicatedListItemException(typeof(TSource));
                }

                if (EntityBaseProxy.IdIsEmpty(s))
                {
                    target.Add(MapToNewTarget<TSource, TTarget>(s, false, newTargetTracker, keepUnmatched));
                }
                else
                {
                    var t = target.FirstOrDefault(i => Equals(EntityBaseProxy.GetId(i), EntityBaseProxy.GetId(s)));
                    if (t != default)
                    {
                        Map(s, t, false, newTargetTracker, _mappingContext, keepUnmatched);
                        shadowSet.Remove(t);
                    }
                    else
                    {
                        throw new EntityNotFoundException(typeof(TTarget), EntityBaseProxy.GetId(s));
                    }
                }
            }
        }

        if (!(keepUnmatched.HasValue && keepUnmatched.Value))
        {
            foreach (var toBeRemoved in shadowSet)
            {
                target.Remove(toBeRemoved);
                _entityRemover.RemoveIfConfigured(_databaseContext, toBeRemoved, _mappingContext.MakeMappingData(propertyName));
            }
        }
    }

    private static Expression<Func<TEntity, bool>> BuildIdEqualsExpression<TEntity>(
        IIdPropertyTracker identityPropertyTracker,
        IScalarTypeConverter scalarConverter,
        object? value)
       where TEntity : class
    {
        var parameter = Expression.Parameter(typeof(TEntity), "entity");
        var identityProperty = identityPropertyTracker.GetIdProperty<TEntity>();

        var equal = identityProperty.PropertyType.IsInstanceOfType(value) ?
            Expression.Equal(
                Expression.Property(parameter, identityProperty),
                Expression.Convert(Expression.Constant(value), identityProperty.PropertyType))
            : Expression.Equal(
                Expression.Property(parameter, identityProperty),
                Expression.Constant(scalarConverter.Convert(value, identityProperty.PropertyType)));
        return Expression.Lambda<Func<TEntity, bool>>(equal, parameter);
    }

    private TTarget MapToNewTarget<TSource, TTarget>(TSource source, bool mapKeyProperties, INewTargetTracker<int>? newTargetTracker, bool? keepUnmatched)
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
            Map(source, newTarget, mapKeyProperties, newTargetTracker, _mappingContext, keepUnmatched);
            _databaseContext.Set<TTarget>().Add(newTarget);
        }

        return newTarget;
    }

    private TTarget AttachTarget<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class
    {
        var target = _entityFactory.Make<TTarget>();
        var mapperSet = _lookup.LookUp(typeof(TSource), typeof(TTarget));
        if (mapperSet?.keyMapper != null)
        {
            ((Utilities.MapScalarProperties<TSource, TTarget>)mapperSet.Value.keyMapper)(source, target, _scalarConverter);
        }

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

    public override TTarget? MapEntityProperty<TSource, TTarget>(TSource? source, TTarget? target, INewTargetTracker<int>? newTargetTracker, string propertyName, bool? keepUnmatched)
        where TSource : class
        where TTarget : class
    {
        if (source != default)
        {
            if (target != default)
            {
                Map(source, target, true, newTargetTracker);
                return target;
            }

            return MapToNewTarget<TSource, TTarget>(source, newTargetTracker);
        }

        return default;
    }

    public override void MapListProperty<TSource, TTarget>(ICollection<TSource>? source, ICollection<TTarget> target, INewTargetTracker<int>? newTargetTracker, string propertyName, bool? keepUnmatched)
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

                target.Add(MapToNewTarget<TSource, TTarget>(s, newTargetTracker));
            }
        }
    }

    private TTarget MapToNewTarget<TSource, TTarget>(TSource source, INewTargetTracker<int>? newTargetTracker)
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
            Map(source, newTarget, true, newTargetTracker);
        }

        return newTarget;
    }
}