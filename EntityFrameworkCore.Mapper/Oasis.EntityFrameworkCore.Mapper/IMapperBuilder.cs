namespace Oasis.EntityFrameworkCore.Mapper;

using System.Linq.Expressions;

public interface IMapperBuilder
{
    public const bool DefaultKeepEntityOnMappingRemoved = false;

    IMapperBuilder WithConfiguration<T>(TypeConfiguration configuration)
        where T : class;

    IMapperBuilder WithScalarMapper<TSource, TTarget>(Expression<Func<TSource?, TTarget?>> expression)
        where TSource : notnull
        where TTarget : notnull;

    IMapperBuilder Register<TSource, TTarget>()
        where TSource : class
        where TTarget : class;

    IMapperBuilder RegisterTwoWay<TSource, TTarget>()
        where TSource : class
        where TTarget : class;

    IMapper Build(string? defaultIdPropertyName = default, string? defaultTimeStampPropertyName = default);
}

public record struct TypeConfiguration(string? identityPropertyName = null, string? timestampPropertyName = null, bool keepEntityOnMappingRemoved = IMapperBuilder.DefaultKeepEntityOnMappingRemoved);
