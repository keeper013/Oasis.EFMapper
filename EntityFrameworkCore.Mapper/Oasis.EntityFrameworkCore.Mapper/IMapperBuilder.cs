namespace Oasis.EntityFrameworkCore.Mapper;

using System.Linq.Expressions;

public interface IMapperBuilder
{
    public const bool DefaultKeepEntityOnMappingRemoved = false;

    IMapperBuilder WithFactoryMethod<TEntity>(Expression<Func<TEntity>> factoryMethod)
        where TEntity : class;

    IMapperBuilder WithConfiguration<TEntity>(TypeConfiguration configuration)
        where TEntity : class;

    IMapperBuilder WithScalarConverter<TSource, TTarget>(Expression<Func<TSource?, TTarget?>> expression)
        where TSource : notnull
        where TTarget : notnull;

    IMapperBuilder Register<TSource, TTarget>()
        where TSource : class
        where TTarget : class;

    IMapperBuilder RegisterTwoWay<TSource, TTarget>()
        where TSource : class
        where TTarget : class;

    IMapper Build();
}

public record struct TypeConfiguration(
    string? identityPropertyName = null,
    string? timestampPropertyName = null,
    bool keepEntityOnMappingRemoved = IMapperBuilder.DefaultKeepEntityOnMappingRemoved);