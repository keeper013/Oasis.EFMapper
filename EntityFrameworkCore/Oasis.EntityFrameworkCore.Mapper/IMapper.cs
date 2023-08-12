namespace Oasis.EntityFrameworkCore.Mapper;

using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

public interface IMapper
{
    IMappingSession CreateMappingSession();

    TTarget Map<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class;

    IMappingToDatabaseSession CreateMappingToDatabaseSession(DbContext databaseContext);

    Task<TTarget> MapAsync<TSource, TTarget>(TSource source, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer, DbContext databaseContext)
        where TSource : class
        where TTarget : class;
}

public interface IMappingToDatabaseSession
{
    Task<TTarget> MapAsync<TSource, TTarget>(TSource source, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer)
        where TSource : class
        where TTarget : class;
}

public interface IMappingSession
{
    TTarget Map<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class;
}
