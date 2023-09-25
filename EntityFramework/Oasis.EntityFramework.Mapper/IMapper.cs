namespace Oasis.EntityFramework.Mapper;

using System.Data.Entity;
using System.Linq.Expressions;

public interface IToMemoryMapper
{
    TTarget Map<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class;
}

public interface IToMemorySession : IToMemoryMapper
{
    void Reset();
}

public interface IToDatabaseMapper
{
    DbContext DatabaseContext { set; }

    Task<TTarget> MapAsync<TSource, TTarget>(TSource source, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer)
        where TSource : class
        where TTarget : class;
}

public interface IToDatabaseSession : IToDatabaseMapper
{
    void Reset();
}

public interface IMapper : IToDatabaseMapper, IToMemoryMapper
{
}

public interface IMapperFactory
{
    IMapper MakeMapper(DbContext? databaseContext = null);

    IToMemoryMapper MakeToMemoryMapper();

    IToMemorySession MakeToMemorySession();

    IToDatabaseMapper MakeToDatabaseMapper(DbContext? databaseContext = null);

    IToDatabaseSession MakeToDatabaseSession(DbContext? databaseContext = null);
}
