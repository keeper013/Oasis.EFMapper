namespace Oasis.EntityFrameworkCore.Mapper;

using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

public interface IMapper
{
    IMappingFromEntitiesSession CreateMappingFromEntitiesSession();

    IMappingToEntitiesSession CreateMappingToEntitiesSession(DbContext databaseContext);
}

public interface IMappingToEntitiesSession
{
    Task<TTarget> MapAsync<TSource, TTarget>(TSource source, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>> includer)
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase, new();
}

public interface IMappingFromEntitiesSession
{
    TTarget Map<TSource, TTarget>(TSource source)
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase, new();
}
