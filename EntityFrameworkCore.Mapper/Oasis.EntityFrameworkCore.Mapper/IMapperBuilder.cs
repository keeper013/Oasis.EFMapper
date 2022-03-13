namespace Oasis.EntityFrameworkCore.Mapper;

public interface IMapperBuilder
{
    IMapperBuilder WithScalarMapper<TSource, TTarget>(Func<TSource, TTarget> func);

    IMapperBuilder Register<TSource, TTarget>()
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase;

    IMapper Build();
}
