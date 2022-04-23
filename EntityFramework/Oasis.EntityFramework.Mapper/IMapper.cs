namespace Oasis.EntityFramework.Mapper;

using System.Data.Entity;
using System.Linq.Expressions;

public interface IMapper
{
    IMappingSession CreateMappingSession();

    TTarget Map<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class;

    IMappingToDatabaseSession CreateMappingToDatabaseSession(DbContext databaseContext);

    Task<TTarget> MapAsync<TSource, TTarget>(TSource source, DbContext databaseContext, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer = default)
        where TSource : class
        where TTarget : class;
}

public interface IMappingToDatabaseSession
{
    Task<TTarget> MapAsync<TSource, TTarget>(TSource source, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer = default)
        where TSource : class
        where TTarget : class;
}

public interface IMappingSession
{
    TTarget Map<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class;
}
