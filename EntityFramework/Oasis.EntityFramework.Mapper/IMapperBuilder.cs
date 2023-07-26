namespace Oasis.EntityFramework.Mapper;

using System.Linq.Expressions;

public interface IMapperBuilder
{
    IMapperBuilder WithFactoryMethod<TList, TItem>(Expression<Func<TList>> factoryMethod, bool throwIfRedundant = false)
        where TList : class, ICollection<TItem>
        where TItem : class;

    IMapperBuilder WithFactoryMethod<TEntity>(Expression<Func<TEntity>> factoryMethod, bool throwIfRedundant = false)
        where TEntity : class;

    IMapperBuilder WithConfiguration<TEntity>(EntityConfiguration configuration, bool throwIfRedundant = false)
        where TEntity : class;

    IMapperBuilder WithScalarConverter<TSource, TTarget>(Expression<Func<TSource, TTarget>> expression, bool throwIfRedundant = false);

    IMapperBuilder Register<TSource, TTarget>(ICustomTypeMapperConfiguration? configuration = null)
        where TSource : class
        where TTarget : class;

    IMapperBuilder RegisterTwoWay<TSource, TTarget>(
        ICustomTypeMapperConfiguration? sourceToTargetConfiguration = null,
        ICustomTypeMapperConfiguration? targetToSourceConfiguration = null)
        where TSource : class
        where TTarget : class;

    IMapper Build();
}

public record struct EntityConfiguration(
    string identityPropertyName,
    string? concurrencyTokenPropertyName = default,
    bool? keepEntityOnMappingRemoved = default,
    string[]? excludedProperties = default);