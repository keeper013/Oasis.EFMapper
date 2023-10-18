namespace Oasis.EntityFrameworkCore.Mapper;

using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

public interface IMapperSessionHandler
{
    bool IsSessionOn { get; }

    void StartSession();

    void StopSession();
}

public interface IToMemoryMapper : IMapperSessionHandler
{
    TTarget Map<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class;

    void Map<TSource, TTarget>(TSource source, TTarget target)
        where TSource : class
        where TTarget : class;
}

public interface IToDatabaseMapper : IMapperSessionHandler
{
    DbContext DatabaseContext { set; }

    Task<TTarget> MapAsync<TSource, TTarget>(TSource source, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer)
        where TSource : class
        where TTarget : class;
}

public interface IMapper : IToDatabaseMapper, IToMemoryMapper
{
}

public interface IMapperFactory
{
    IMapper MakeMapper(DbContext? databaseContext = null);

    IToMemoryMapper MakeToMemoryMapper();

    IToDatabaseMapper MakeToDatabaseMapper(DbContext? databaseContext = null);
}
