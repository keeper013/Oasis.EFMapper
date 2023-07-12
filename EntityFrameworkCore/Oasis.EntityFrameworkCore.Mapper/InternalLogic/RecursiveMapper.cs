namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Linq.Expressions;

internal abstract class RecursiveMapper : IEntityPropertyMapper<int>, IListPropertyMapper<int>
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

    public abstract TTarget? MapEntityProperty<TSource, TTarget>(TSource? source, TTarget? target, INewTargetTracker<int> newTargetTracker)
        where TSource : class
        where TTarget : class;

    public abstract void MapListProperty<TSource, TTarget>(ICollection<TSource>? source, ICollection<TTarget> target, INewTargetTracker<int> newTargetTracker)
        where TSource : class
        where TTarget : class;

    TList IListPropertyMapper<int>.ConstructListType<TList, TItem>()
    {
        return _listTypeConstructor.Construct<TList, TItem>();
    }

    internal void Map<TSource, TTarget>(TSource source, TTarget target, bool mapKeyProperties, INewTargetTracker<int> newTargetTracker)
        where TSource : class
        where TTarget : class
    {
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

        var mapperSet = _lookup.LookUp(typeof(TSource), typeof(TTarget));
        if (mapperSet.customPropertiesMapper != null)
        {
            ((Action<TSource, TTarget>)mapperSet.customPropertiesMapper)(source, target);
        }

        if (mapKeyProperties)
        {
            ((Utilities.MapScalarProperties<TSource, TTarget>)mapperSet.keyPropertiesMapper)(source, target, _scalarConverter);
        }

        ((Utilities.MapScalarProperties<TSource, TTarget>)mapperSet.scalarPropertiesMapper)(source, target, _scalarConverter);
        ((Utilities.MapEntityProperties<TSource, TTarget, int>)mapperSet.entityPropertiesMapper)(this, source, target, newTargetTracker);
        ((Utilities.MapListProperties<TSource, TTarget, int>)mapperSet.listPropertiesMapper)(this, source, target, newTargetTracker);
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
    private readonly DbContext _databaseContext;

    public ToDatabaseRecursiveMapper(
        IScalarTypeConverter scalarConverter,
        IListTypeConstructor listTypeConstructor,
        MapperSetLookUp lookup,
        EntityBaseProxy entityBaseProxy,
        IEntityFactory entityFactory,
        DbContext databaseContext)
        : base(scalarConverter, listTypeConstructor, lookup, entityBaseProxy)
    {
        _scalarConverter = scalarConverter;
        _entityFactory = entityFactory;
        _lookup = lookup;
        _entityBaseProxy = entityBaseProxy;
        _databaseContext = databaseContext;
    }

    public async Task<TTarget> MapAsync<TSource, TTarget>(
        TSource source,
        Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer,
        MapToDatabaseType mappingType,
        NewTargetTracker<int> newEntityTracker)
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
                        Map(source, target, true, newEntityTracker);
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

        if (!newEntityTracker.NewTargetIfNotExist(source.GetHashCode(), out TTarget newTarget))
        {
            Map(source, newTarget, sourceHasId, newEntityTracker);
            _databaseContext.Set<TTarget>().Add(newTarget);
        }

        return newTarget;
    }

    public override TTarget? MapEntityProperty<TSource, TTarget>(TSource? source, TTarget? target, INewTargetTracker<int> newTargetTracker)
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

            if (!newTargetTracker.NewTargetIfNotExist<TTarget>(source.GetHashCode(), out var n))
            {
                Map(source, n, false, newTargetTracker);
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

        Map(source, target, false, newTargetTracker);
        return target;
    }

    public override void MapListProperty<TSource, TTarget>(ICollection<TSource>? source, ICollection<TTarget> target, INewTargetTracker<int> newTargetTracker)
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
                    if (!newTargetTracker.NewTargetIfNotExist<TTarget>(s.GetHashCode(), out var n))
                    {
                        Map(s, n, false, newTargetTracker);
                        _databaseContext.Set<TTarget>().Add(n);
                    }

                    target.Add(n);
                }
                else
                {
                    var t = target.FirstOrDefault(i => Equals(EntityBaseProxy.GetId(i), EntityBaseProxy.GetId(s)));
                    if (t != default)
                    {
                        Map(s, t, false, newTargetTracker);
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

internal sealed class ToMemoryRecursiveMapper : RecursiveMapper
{
    public ToMemoryRecursiveMapper(
        IScalarTypeConverter scalarConverter,
        IListTypeConstructor listTypeConstructor,
        MapperSetLookUp lookup,
        EntityBaseProxy entityBaseProxy)
        : base(scalarConverter, listTypeConstructor, lookup, entityBaseProxy)
    {
    }

    public override TTarget? MapEntityProperty<TSource, TTarget>(TSource? source, TTarget? target, INewTargetTracker<int> newTargetTracker)
        where TSource : class
        where TTarget : class
    {
        if (source != default)
        {
            if (!newTargetTracker.NewTargetIfNotExist<TTarget>(source.GetHashCode(), out var n))
            {
                Map(source, n, true, newTargetTracker);
            }

            return n;
        }

        return default;
    }

    public override void MapListProperty<TSource, TTarget>(ICollection<TSource>? source, ICollection<TTarget> target, INewTargetTracker<int> newTargetTracker)
    {
        if (source != default)
        {
            foreach (var s in source)
            {
                if (!newTargetTracker.NewTargetIfNotExist<TTarget>(s.GetHashCode(), out var n))
                {
                    Map(s, n, true, newTargetTracker);
                }

                target.Add(n);
            }
        }
    }
}