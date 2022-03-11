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
            if (!_context.TryGetTargetIfNotTracked<TTarget>(source.GetHashCode(), out target))
            {
                _context.DatabaseContext.Set<TTarget>().Add(target!);
                MapInternal(source, target!);
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

internal sealed class MappingContext : INewSourceEntityTracker, IDisposable
{
    private readonly ISet<int> _newEntitiesHashCodes = new HashSet<int>();
    private DbContext? _dbContext;

    public bool IsStarted => _dbContext != null;

    public DbContext DatabaseContext
    {
        get { return _dbContext ?? throw new MappingContextNotStartedException(); }
        set { _dbContext = value; }
    }

    public bool TryGetTargetIfNotTracked<TTarget>(int hashCode, out TTarget? target)
        where TTarget : class, new()
    {
        if (_newEntitiesHashCodes.Add(hashCode))
        {
            target = new TTarget();
            return true;
        }

        target = default;
        return false;
    }

    public void Dispose()
    {
        _newEntitiesHashCodes.Clear();
        _dbContext = null;
    }
}
