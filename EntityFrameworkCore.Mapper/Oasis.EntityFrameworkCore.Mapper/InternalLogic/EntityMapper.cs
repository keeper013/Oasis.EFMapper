namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Linq;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Purposely designed this way.")]
internal sealed class EntityMapper : IEntityMapper
{
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> _mappers;
    private readonly MappingContext _context;

    public EntityMapper(IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> mappers)
    {
        _mappers = mappers;
        _context = new MappingContext();
    }

    public IDisposable StartMappingContext(DbContext databaseContext)
    {
        if (_context.IsStarted)
        {
            throw new MappingContextStartedException();
        }

        _context.DatabaseContext = databaseContext;

        return _context;
    }

    void IEntityMapper.Map<TSource, TTarget>(TSource source, TTarget target)
    {
        if (!_context.IsStarted)
        {
            throw new MappingContextNotStartedException();
        }

        if (source.Id != target.Id)
        {
            throw new EntityIdNotMatchException(source.Id, target.Id);
        }

        if (source.Id.HasValue)
        {
            if (source.Timestamp == null)
            {
                throw new MissingTimestampException(typeof(TSource), source.Id.Value);
            }

            if (target.Timestamp == null)
            {
                throw new MissingTimestampException(typeof(TTarget), target.Id!.Value);
            }

            if (!Enumerable.SequenceEqual(source.Timestamp, target.Timestamp))
            {
                throw new StaleEntityException(typeof(TSource), source.Id!.Value);
            }
        }

        MapInternal(source, target);
    }

    async Task IEntityMapper.Map<TSource, TTarget>(TSource source, Func<IQueryable<TTarget>, IQueryable<TTarget>> includer)
    {
        if (!_context.IsStarted)
        {
            throw new MappingContextNotStartedException();
        }

        TTarget? target;
        if (source.Id.HasValue)
        {
            target = await includer.Invoke(_context.DatabaseContext.Set<TTarget>()).SingleOrDefaultAsync(t => t.Id == source.Id);
            if (target == null)
            {
                throw new EntityNotFoundException(typeof(TTarget), source.Id.Value);
            }

            if (target.Timestamp == null)
            {
                throw new MissingTimestampException(typeof(TTarget), source.Id.Value);
            }

            if (!Enumerable.SequenceEqual(target.Timestamp!, source.Timestamp!))
            {
                throw new StaleEntityException(typeof(TTarget), source.Id.Value);
            }

            MapInternal(source, target);
        }
        else
        {
            if (!_context.NewTargetIfNotExist<TTarget>(source.GetHashCode(), out target))
            {
                MapInternal(source, target!);
                _context.DatabaseContext.Set<TTarget>().Add(target!);
            }
        }
    }

    private void MapInternal<TSource, TTarget>(TSource source, TTarget target)
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase
    {
        new RecursiveMapper(_context.DatabaseContext, _context, _mappers).Map(source, target);
    }
}

internal sealed class MappingContext : INewEntityTracker, IDisposable
{
    private readonly IDictionary<int, object> _newEntityDictionary = new Dictionary<int, object>();
    private DbContext? _dbContext;

    public bool IsStarted => _dbContext != null;

    public DbContext DatabaseContext
    {
        get { return _dbContext ?? throw new MappingContextNotStartedException(); }
        set { _dbContext = value; }
    }

    public bool NewTargetIfNotExist<TTarget>(int hashCode, out TTarget? target)
        where TTarget : class, new()
    {
        bool result = false;
        object? obj;
        target = default;
        lock (_newEntityDictionary)
        {
            result = _newEntityDictionary.TryGetValue(hashCode, out obj);
            if (!result)
            {
                target = new TTarget();
                _newEntityDictionary.Add(hashCode, target);
            }
        }

        if (result)
        {
            if (obj is TTarget)
            {
                target = obj as TTarget;
            }
            else
            {
                throw new MultipleMappingException(obj!.GetType(), typeof(TTarget));
            }
        }

        return result;
    }

    public void Dispose()
    {
        _newEntityDictionary.Clear();
        _dbContext = null;
    }
}
