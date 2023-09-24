namespace Oasis.EntityFramework.Mapper;

using System.Linq.Expressions;

public interface IEntityConfiguration<TEntity> : IConfigurator<IMapperBuilder>
    where TEntity : class
{
    IEntityConfiguration<TEntity> SetIdentityPropertyName(string identityPropertyName);

    IEntityConfiguration<TEntity> SetConcurrencyTokenPropertyName(string concurrencyTokenPropertyName);

    IEntityConfiguration<TEntity> SetKeyPropertyNames(string identityPropertyName, string? concurrencyTokenPropertyName = null);

    IEntityConfiguration<TEntity> ExcludePropertiesByName(params string[] names);

    IEntityConfiguration<TEntity> KeepUnmatched(params string[] names);
}

public interface ICustomTypeMapperConfiguration<TSource, TTarget> : IConfigurator<IMapperBuilder>
    where TSource : class
    where TTarget : class
{
    ICustomTypeMapperConfiguration<TSource, TTarget> SetMapToDatabaseType(MapToDatabaseType mapToDatabase);

    ICustomTypeMapperConfiguration<TSource, TTarget> MapProperty<TProperty>(Expression<Func<TTarget, TProperty>> setter, Func<TSource, TProperty> value);

    ICustomTypeMapperConfiguration<TSource, TTarget> ExcludePropertiesByName(params string[] names);

    ICustomTypeMapperConfiguration<TSource, TTarget> KeepUnmatched(params string[] names);
}

public interface IMapperBuilder
{
    IMapperBuilder WithFactoryMethod<TEntity>(Func<TEntity> factoryMethod, bool throwIfRedundant = false)
        where TEntity : class;

    IEntityConfiguration<TEntity> Configure<TEntity>()
        where TEntity : class;

    ICustomTypeMapperConfiguration<TSource, TTarget> Configure<TSource, TTarget>()
        where TSource : class
        where TTarget : class;

    IMapperBuilder WithScalarConverter<TSource, TTarget>(Func<TSource, TTarget> func, bool throwIfRedundant = false);

    IMapperBuilder Register<TSource, TTarget>()
        where TSource : class
        where TTarget : class;

    IMapperBuilder RegisterTwoWay<TSource, TTarget>()
        where TSource : class
        where TTarget : class;

    IMapperFactory Build();
}