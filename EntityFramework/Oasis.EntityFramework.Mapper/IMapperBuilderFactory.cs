namespace Oasis.EntityFramework.Mapper;

public interface IMapperBuilderFactory
{
    IMapperBuilder MakeMapperBuilder(
        string? identityPropertyName = default,
        string? concurrencyTokenPropertyName = default,
        string[]? excludedProperties = default,
        bool? keepEntityOnMappingRemoved = default);

    ICustomTypeMapperConfigurationBuilder<TSource, TTarget> MakeCustomTypeMapperBuilder<TSource, TTarget>()
        where TSource : class
        where TTarget : class;
}
