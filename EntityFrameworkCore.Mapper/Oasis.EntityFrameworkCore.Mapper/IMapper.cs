namespace Oasis.EntityFrameworkCore.Mapper;

using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

public interface IMapper
{
    IMappingSession CreateMappingSession();

    IMappingToDatabaseSession CreateMappingToDatabaseSession(DbContext databaseContext);
}

public interface IMappingToDatabaseSession
{
    Task<TTarget> MapAsync<TSource, TTarget>(TSource source, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer = default)
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase, new();
}

public interface IMappingSession
{
    TTarget Map<TSource, TTarget>(TSource source)
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase, new();
}
