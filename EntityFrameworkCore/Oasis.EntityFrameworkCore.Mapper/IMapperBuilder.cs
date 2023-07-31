namespace Oasis.EntityFrameworkCore.Mapper;

using System.Linq.Expressions;

public interface IMapperBuilder
{
    public const bool DefaultKeepEntityOnMappingRemoved = true;

    IMapperBuilder WithFactoryMethod<TList, TItem>(Expression<Func<TList>> factoryMethod, bool throwIfRedundant = false)
        where TList : class, ICollection<TItem>
        where TItem : class;

    IMapperBuilder WithFactoryMethod<TEntity>(Expression<Func<TEntity>> factoryMethod, bool throwIfRedundant = false)
        where TEntity : class;

    IMapperBuilder WithConfiguration<TEntity>(
        string? identityPropertyName = default,
        string? concurrencyTokenPropertyName = default,
        string[]? excludedProperties = default,
        bool? keepEntityOnMappingRemoved = default,
        bool throwIfRedundant = false)
        where TEntity : class;

    IMapperBuilder WithScalarConverter<TSource, TTarget>(Expression<Func<TSource, TTarget>> expression, bool throwIfRedundant = false);

    IMapperBuilder Register<TSource, TTarget>(ICustomTypeMapperConfiguration<TSource, TTarget>? configuration = null)
        where TSource : class
        where TTarget : class;

    IMapperBuilder RegisterTwoWay<TSource, TTarget>(
        ICustomTypeMapperConfiguration<TSource, TTarget>? sourceToTargetConfiguration = null,
        ICustomTypeMapperConfiguration<TTarget, TSource>? targetToSourceConfiguration = null)
        where TSource : class
        where TTarget : class;

    IMapper Build();
}