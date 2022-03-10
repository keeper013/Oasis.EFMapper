namespace Oasis.EntityFrameworkCore.Mapper;

public interface IEntityMapperBuilder
{
    void Register<TSource, TTarget>()
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase;

    IEntityMapper Build();
}
