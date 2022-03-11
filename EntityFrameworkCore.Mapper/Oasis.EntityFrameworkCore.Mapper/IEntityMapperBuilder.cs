namespace Oasis.EntityFrameworkCore.Mapper;

public interface IEntityMapperBuilder
{
    IEntityMapperBuilder WithScalarMapper<TSource, TTarget>(Func<TSource, TTarget> func);

    IEntityMapperBuilder Register<TSource, TTarget>()
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase;

    IEntityMapper Build();
}
