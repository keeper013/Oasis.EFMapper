namespace Oasis.EntityFrameworkCore.Mapper;

using Microsoft.EntityFrameworkCore;

public interface IEntityMapper
{
    IDisposable StartMappingContext(DbContext databaseContext);

    void Map<TSource, TTarget>(TSource source, TTarget target)
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase;

    Task Map<TSource, TTarget>(TSource source, Func<IQueryable<TTarget>, IQueryable<TTarget>> includer)
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase, new();
}
