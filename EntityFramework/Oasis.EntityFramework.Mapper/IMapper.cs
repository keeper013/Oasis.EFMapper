namespace Oasis.EntityFramework.Mapper;

using System.Data.Entity;
using System.Linq.Expressions;

[Flags]
public enum MapToDatabaseType : byte
{
    /// <summary>
    /// Insert
    /// </summary>
    Insert = 1,

    /// <summary>
    /// Update
    /// </summary>
    Update = 2,

    /// <summary>
    /// Insert or Update
    /// </summary>
    Upsert = Insert | Update,
}

public interface IMapper
{
    IMappingSession CreateMappingSession();

    TTarget Map<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class;

    IMappingToDatabaseSession CreateMappingToDatabaseSession(DbContext databaseContext);

    Task<TTarget> MapAsync<TSource, TTarget>(TSource source, DbContext databaseContext, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer = default, MapToDatabaseType mappingType = MapToDatabaseType.Upsert)
        where TSource : class
        where TTarget : class;
}

public interface IMappingToDatabaseSession
{
    Task<TTarget> MapAsync<TSource, TTarget>(TSource source, Expression<Func<IQueryable<TTarget>, IQueryable<TTarget>>>? includer = default, MapToDatabaseType mappingType = MapToDatabaseType.Upsert)
        where TSource : class
        where TTarget : class;
}

public interface IMappingSession
{
    TTarget Map<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class;
}
