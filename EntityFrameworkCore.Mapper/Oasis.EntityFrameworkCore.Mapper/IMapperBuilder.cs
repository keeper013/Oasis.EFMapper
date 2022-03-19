namespace Oasis.EntityFrameworkCore.Mapper;

using System.Linq.Expressions;

public interface IMapperBuilder
{
    IMapperBuilder WithConfiguration<T>(TypeConfiguration configuration)
        where T : class;

    IMapperBuilder WithScalarMapper<TSource, TTarget>(Expression<Func<TSource, TTarget>> expression);

    IMapperBuilder Register<TSource, TTarget>()
        where TSource : class
        where TTarget : class;

    IMapperBuilder RegisterTwoWay<TSource, TTarget>()
        where TSource : class
        where TTarget : class;

    IMapper Build(string? defaultIdPropertyName = default, string? defaultTimeStampPropertyName = default);
}

public record struct TypeConfiguration(string? identityColumnName = null, string? timestampColumnName = null, bool keepEntityOnMappingRemoved = false);
