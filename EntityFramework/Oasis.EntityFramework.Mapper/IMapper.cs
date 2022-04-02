namespace Oasis.EntityFramework.Mapper;

using System.Data.Entity;
using System.Linq.Expressions;

public interface IMapper
{
    IMappingSession CreateMappingSession();

    IMappingToDatabaseSession CreateMappingToDatabaseSession(DbContext databaseContext);
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
