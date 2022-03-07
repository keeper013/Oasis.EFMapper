﻿namespace Oasis.EfMapper;

using Microsoft.EntityFrameworkCore;
using Oasis.EfMapper.Exceptions;
using System.Linq;

internal sealed class EntityMapper : IEntityMapper
{
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> _mappers;

    public EntityMapper(
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> mappers)
    {
        this._mappers = mappers;
    }

    void IEntityMapper.Map<TSource, TTarget>(TSource source, TTarget target, DbContext dbContext)
    {
        MapInternal(source, target, dbContext);
    }

    async Task IEntityMapper.Map<TSource, TTarget>(TSource source, Func<IQueryable<TTarget>, IQueryable<TTarget>> includer, DbContext dbContext)
    {
        if (source.Id.HasValue)
        {
            var target = await includer.Invoke(dbContext.Set<TTarget>()).SingleOrDefaultAsync(t => t.Id == source.Id);
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

            MapInternal(source, target, dbContext);
        }
        else
        {
            var target = new TTarget();
            dbContext.Set<TTarget>().Add(target);
            MapInternal(source, target, dbContext);
        }
    }

    private void MapInternal<TSource, TTarget>(TSource source, TTarget target, DbContext dbContext)
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase
    {
        Utilities.RecursivelyMap(source, target, new MappingContext(dbContext, _mappers));
    }
}
